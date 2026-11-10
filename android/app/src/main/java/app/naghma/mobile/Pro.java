package app.naghma.mobile;

import android.content.Context;
import android.content.SharedPreferences;

import java.nio.charset.StandardCharsets;
import java.security.KeyFactory;
import java.security.PublicKey;
import java.security.Signature;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;

/**
 * Same key format as the Windows app, so one purchase covers both:
 *
 *   NAGHMA-&lt;base64url(payload)&gt;.&lt;base64url(signature)&gt;
 *   payload = {"e":"buyer@mail","t":1735689600,"p":"pro"}
 *
 * Only the ECDSA P-256 public key ships here. A rewarded ad grants 24 hours of Pro.
 */
public final class Pro {

    private static final String SPKI =
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE56lm9ss3NgQ2MMUNgmvQ5xVhQaGxNRcOGyIHHSv5mcnQ1/QOMxiQc47uudHkfHWXnlrFyLEd5Tq4zu01FSfyRQ==";

    private static final String PREFS = "naghma";
    private static final String K_KEY = "pro_key";
    private static final String K_REWARD = "pro_reward_until";

    private Pro() { }

    private static SharedPreferences prefs(Context c) {
        return c.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    /** Permanent key, verified offline. */
    public static String email(Context c) {
        String key = prefs(c).getString(K_KEY, null);
        return key == null ? "" : verify(key);
    }

    public static boolean hasKey(Context c) {
        return !email(c).isEmpty();
    }

    public static long rewardUntil(Context c) {
        return prefs(c).getLong(K_REWARD, 0L);
    }

    public static boolean isPro(Context c) {
        return hasKey(c) || rewardUntil(c) > System.currentTimeMillis();
    }

    public static void saveKey(Context c, String key) {
        prefs(c).edit().putString(K_KEY, key).apply();
    }

    public static void clearKey(Context c) {
        prefs(c).edit().remove(K_KEY).apply();
    }

    public static void grantReward(Context c, long hours) {
        long base = Math.max(System.currentTimeMillis(), rewardUntil(c));
        prefs(c).edit().putLong(K_REWARD, base + hours * 3600_000L).apply();
    }

    public static long rewardHoursLeft(Context c) {
        long ms = rewardUntil(c) - System.currentTimeMillis();
        return ms <= 0 ? 0 : (long) Math.ceil(ms / 3600_000.0);
    }

    /** @return the buyer email when the key checks out, otherwise "". */
    public static String verify(String raw) {
        try {
            String text = raw.trim().replace("\r", "").replace("\n", "").replace(" ", "");
            if (!text.regionMatches(true, 0, "NAGHMA-", 0, 7)) return "";

            String rest = text.substring(7);
            int dot = rest.indexOf('.');
            if (dot <= 0 || dot == rest.length() - 1) return "";

            byte[] payload = unb64(rest.substring(0, dot));
            byte[] sig = unb64(rest.substring(dot + 1));

            byte[] der = new X509EncodedKeySpec(Base64.getDecoder().decode(SPKI)).getEncoded();
            PublicKey pub = KeyFactory.getInstance("EC").generatePublic(new X509EncodedKeySpec(der));

            Signature s = Signature.getInstance("SHA256withECDSA");
            s.initVerify(pub);
            s.update(payload);
            if (!s.verify(p1363ToDer(sig))) return "";

            String json = new String(payload, StandardCharsets.UTF_8);
            if (!json.contains("\"pro\"")) return "";
            int i = json.indexOf("\"e\"");
            if (i < 0) return "";
            int q1 = json.indexOf('"', json.indexOf(':', i) + 1);
            int q2 = json.indexOf('"', q1 + 1);
            return (q1 < 0 || q2 < 0) ? "" : json.substring(q1 + 1, q2);
        } catch (Throwable t) {
            return "";
        }
    }

    private static byte[] unb64(String s) {
        String t = s.replace('-', '+').replace('_', '/');
        switch (t.length() % 4) {
            case 2: t += "=="; break;
            case 3: t += "="; break;
            default: break;
        }
        return Base64.getDecoder().decode(t);
    }

    /** .NET signs with IEEE P1363 (r||s); Java wants DER. */
    private static byte[] p1363ToDer(byte[] sig) {
        if (sig.length % 2 != 0) return sig;
        int half = sig.length / 2;
        byte[] r = trim(sig, 0, half);
        byte[] s = trim(sig, half, half);
        int len = 2 + r.length + 2 + s.length;
        byte[] out = new byte[2 + len];
        out[0] = 0x30;
        out[1] = (byte) len;
        out[2] = 0x02;
        out[3] = (byte) r.length;
        System.arraycopy(r, 0, out, 4, r.length);
        int o = 4 + r.length;
        out[o] = 0x02;
        out[o + 1] = (byte) s.length;
        System.arraycopy(s, 0, out, o + 2, s.length);
        return out;
    }

    private static byte[] trim(byte[] src, int off, int len) {
        int start = off;
        while (start < off + len - 1 && src[start] == 0) start++;
        int n = off + len - start;
        boolean pad = (src[start] & 0x80) != 0;
        byte[] out = new byte[n + (pad ? 1 : 0)];
        System.arraycopy(src, start, out, pad ? 1 : 0, n);
        return out;
    }
}

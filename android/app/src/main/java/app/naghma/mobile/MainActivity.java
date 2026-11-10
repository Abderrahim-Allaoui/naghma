package app.naghma.mobile;

import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;

import com.google.android.gms.ads.AdError;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.AdView;
import com.google.android.gms.ads.FullScreenContentCallback;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.MobileAds;
import com.google.android.gms.ads.rewarded.RewardedAd;
import com.google.android.gms.ads.rewarded.RewardedAdLoadCallback;
import com.google.android.material.button.MaterialButton;
import com.google.android.material.progressindicator.LinearProgressIndicator;
import com.google.android.material.textfield.MaterialAutoCompleteTextView;
import com.google.android.material.textfield.TextInputEditText;

import java.util.ArrayList;
import java.util.List;

public class MainActivity extends AppCompatActivity implements Engine.Listener {

    private static final String AD_UNIT = "ca-app-pub-3940256099942544/5224354917"; // Google test unit
    private static final String BUY_URL = "https://gumroad.com/l/eydroa";
    private static final int MAX_LOG_LINES = 400;

    private final String[] formats = {
            "Audio — MP3 (best)",
            "Audio — M4A",
            "Audio — OPUS",
            "Audio — FLAC (Pro)",
            "Video — MP4 (Pro 4K)"
    };

    private TextInputEditText input;
    private MaterialAutoCompleteTextView format;
    private MaterialButton download, stop;
    private LinearProgressIndicator progress;
    private TextView status, log, proSub;
    private View proActions, keyRow;
    private EditText key;
    private MaterialButton watchAd, buy, saveKey;

    private Engine engine;
    private final Handler ui = new Handler(Looper.getMainLooper());
    private final StringBuilder logBuffer = new StringBuilder();
    private int logLines;

    private RewardedAd rewarded;
    private boolean loadingAd;
    private AdView adView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        input = findViewById(R.id.input);
        format = findViewById(R.id.format);
        download = findViewById(R.id.download);
        stop = findViewById(R.id.stop);
        progress = findViewById(R.id.progress);
        status = findViewById(R.id.status);
        log = findViewById(R.id.log);
        proSub = findViewById(R.id.proSub);
        proActions = findViewById(R.id.proActions);
        keyRow = findViewById(R.id.keyRow);
        key = findViewById(R.id.key);
        watchAd = findViewById(R.id.watchAd);
        buy = findViewById(R.id.buy);
        saveKey = findViewById(R.id.saveKey);

        format.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_list_item_1, formats));
        format.setText(formats[0], false);

        engine = new Engine(this);

        download.setOnClickListener(v -> start());
        stop.setOnClickListener(v -> {
            engine.cancel();
            stop.setEnabled(false);
            setStatus(getString(R.string.stop) + "…");
        });

        watchAd.setOnClickListener(v -> showRewardAd());
        buy.setOnClickListener(v -> startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(BUY_URL))));
        saveKey.setOnClickListener(v -> saveKey());
        proSub.setOnClickListener(v -> keyRow.setVisibility(
                keyRow.getVisibility() == View.VISIBLE ? View.GONE : View.VISIBLE));

        MobileAds.initialize(this, s -> loadRewardAd());
        refreshPro();

        adView = findViewById(R.id.adView);
        if (!Pro.isPro(this)) {
            adView.setVisibility(View.VISIBLE);
            adView.loadAd(new AdRequest.Builder().build());
        }

        if (savedInstanceState != null) {
            String saved = savedInstanceState.getString("log");
            if (saved != null) {
                log.setText(saved);
                logBuffer.append(saved);
            }
        }
    }

    @Override
    protected void onSaveInstanceState(@NonNull Bundle out) {
        super.onSaveInstanceState(out);
        out.putString("log", log.getText().toString());
    }

    // ------------------------------------------------------------------ download

    private void start() {
        String raw = input.getText() == null ? "" : input.getText().toString().trim();
        if (raw.isEmpty()) {
            input.setError(getString(R.string.tagline));
            return;
        }

        List<String> targets = new ArrayList<>();
        for (String line : raw.split("\\r?\\n")) {
            String t = line.trim();
            if (!t.isEmpty()) targets.add(t);
        }
        if (targets.isEmpty()) return;

        int fmt = indexOf(format.getText().toString());
        boolean pro = Pro.isPro(this);

        if (!pro && (targets.size() > 1 || fmt >= 3)) {
            askPro();
            return;
        }
        if (!pro && fmt > 3) fmt = 4;

        clearLog();
        progress.setIndeterminate(false);
        progress.setProgress(0);
        download.setEnabled(false);
        stop.setEnabled(true);
        setStatus(getString(R.string.preparing));

        engine.run(targets, fmt, pro, this);
    }

    private int indexOf(String label) {
        for (int i = 0; i < formats.length; i++) if (formats[i].equals(label)) return i;
        return 0;
    }

    @Override
    public void onLine(String line) {
        append(line);
    }

    @Override
    public void onProgress(int percent) {
        progress.setProgress(percent);
        setStatus(getString(R.string.downloading) + " " + percent + "%");
    }

    @Override
    public void onItemDone(String target, boolean ok) {
        append(ok ? "  ok  " + target : "  failed  " + target);
    }

    @Override
    public void onAllDone(int ok, int failed) {
        download.setEnabled(true);
        stop.setEnabled(false);
        progress.setProgress(100);
        setStatus(getString(R.string.done, ok + " ok · " + failed + " failed"));
        ui.postDelayed(() -> progress.setProgress(0), 1200);
    }

    // ----------------------------------------------------------------------- ads

    private void loadRewardAd() {
        if (rewarded != null || loadingAd) return;
        loadingAd = true;
        RewardedAd.load(this, AD_UNIT, new AdRequest.Builder().build(),
                new RewardedAdLoadCallback() {
                    @Override
                    public void onAdLoaded(@NonNull RewardedAd ad) {
                        loadingAd = false;
                        rewarded = ad;
                    }

                    @Override
                    public void onAdFailedToLoad(@NonNull LoadAdError error) {
                        loadingAd = false;
                        rewarded = null;
                    }
                });
    }

    private void showRewardAd() {
        if (rewarded == null) {
            loadRewardAd();
            Toast.makeText(this, R.string.pro_reward_failed, Toast.LENGTH_SHORT).show();
            return;
        }

        rewarded.setFullScreenContentCallback(new FullScreenContentCallback() {
            @Override
            public void onAdDismissedFullScreenContent() {
                rewarded = null;
                loadRewardAd();
            }

            @Override
            public void onAdFailedToShowFullScreenContent(@NonNull AdError error) {
                rewarded = null;
                loadRewardAd();
            }
        });

        rewarded.show(this, reward -> {
            Pro.grantReward(MainActivity.this, 24);
            refreshPro();
            Toast.makeText(MainActivity.this, R.string.pro_reward_ok, Toast.LENGTH_LONG).show();
        });
    }

    // ----------------------------------------------------------------------- pro

    private void refreshPro() {
        boolean pro = Pro.isPro(this);
        String mail = Pro.email(this);

        if (Pro.hasKey(this)) {
            proSub.setText(getString(R.string.pro_active) + " · " + mail);
        } else if (pro) {
            proSub.setText(getString(R.string.pro_active) + " · " + Pro.rewardHoursLeft(this) + "h");
        } else {
            proSub.setText(getString(R.string.pro_sub));
        }

        proActions.setVisibility(pro ? View.GONE : View.VISIBLE);
        if (pro) keyRow.setVisibility(View.GONE);

        adView.setVisibility(pro ? View.GONE : View.VISIBLE);
        if (!pro) adView.loadAd(new AdRequest.Builder().build());
    }

    private void saveKey() {
        String value = key.getText().toString().trim();
        if (value.isEmpty()) return;
        String mail = Pro.verify(value);
        if (mail.isEmpty()) {
            Toast.makeText(this, R.string.pro_key_bad, Toast.LENGTH_LONG).show();
            return;
        }
        Pro.saveKey(this, value);
        key.setText("");
        refreshPro();
        Toast.makeText(this, getString(R.string.pro_key_ok, mail), Toast.LENGTH_LONG).show();
    }

    private void askPro() {
        new AlertDialog.Builder(this)
                .setTitle(R.string.pro_needs)
                .setMessage(R.string.pro_needs_body)
                .setPositiveButton(R.string.pro_watch, (d, w) -> showRewardAd())
                .setNeutralButton(R.string.pro_buy, (d, w) ->
                        startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(BUY_URL))))
                .setNegativeButton(android.R.string.cancel, null)
                .show();
    }

    // ----------------------------------------------------------------------- ui

    private void setStatus(String text) {
        status.setText(text);
    }

    private void clearLog() {
        logBuffer.setLength(0);
        logLines = 0;
        log.setText("");
    }

    private void append(String line) {
        if (logLines >= MAX_LOG_LINES) {
            int cut = logBuffer.indexOf("\n", logBuffer.length() / 2);
            if (cut > 0) {
                logBuffer.delete(0, cut + 1);
                logLines = logBuffer.toString().split("\n").length;
            }
        }
        logBuffer.append(line).append('\n');
        logLines++;
        log.setText(logBuffer.toString());
    }
}

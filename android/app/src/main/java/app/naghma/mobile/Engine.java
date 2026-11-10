package app.naghma.mobile;

import android.content.ContentValues;
import android.content.Context;
import android.net.Uri;
import android.os.Environment;
import android.os.Handler;
import android.os.Looper;
import android.provider.MediaStore;
import android.util.Log;

import com.yausername.youtubedl_android.YoutubeDL;
import com.yausername.youtubedl_android.YoutubeDLRequest;

import java.io.File;
import java.io.FileInputStream;
import java.io.OutputStream;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicBoolean;

import kotlin.Unit;

/** Thin wrapper over the bundled yt-dlp: one request per target, results published to Music/. */
public final class Engine {

    private static final String TAG = "Naghma";

    public interface Listener {
        void onLine(String line);
        void onProgress(int percent);
        void onItemDone(String target, boolean ok);
        void onAllDone(int ok, int failed);
    }

    private final Context app;
    private final Handler ui = new Handler(Looper.getMainLooper());
    private final AtomicBoolean cancelled = new AtomicBoolean(false);
    private volatile String processId;

    public Engine(Context context) {
        this.app = context.getApplicationContext();
    }

    public void cancel() {
        cancelled.set(true);
        String id = processId;
        if (id != null) {
            try {
                YoutubeDL.getInstance().destroyProcessById(id);
            } catch (Throwable t) {
                Log.w(TAG, "cancel: " + t.getMessage());
            }
        }
    }

    public void run(List<String> targets, int format, boolean pro, Listener listener) {
        new Thread(() -> {
            int ok = 0;
            int failed = 0;
            File dir = outputDir();
            if (!dir.exists() && !dir.mkdirs()) {
                ui.post(() -> listener.onLine("Cannot create " + dir));
            }

            for (String target : targets) {
                if (cancelled.get()) break;

                String id = UUID.randomUUID().toString();
                processId = id;
                ui.post(() -> listener.onLine("› " + target));

                try {
                    YoutubeDLRequest req = build(target, dir, format, pro);
                    YoutubeDL.getInstance().execute(req, id, (progress, eta, line) -> {
                        if (line != null && !line.trim().isEmpty()) {
                            ui.post(() -> listener.onLine(line.trim()));
                        }
                        if (progress != null && progress >= 0) {
                            int pct = (int) Math.round(progress);
                            ui.post(() -> listener.onProgress(pct));
                        }
                        return Unit.INSTANCE;
                    });
                    publish(dir);
                    ok++;
                    ui.post(() -> listener.onItemDone(target, true));
                } catch (Throwable t) {
                    failed++;
                    String msg = t.getMessage() == null ? t.toString() : t.getMessage();
                    ui.post(() -> listener.onLine("! " + msg));
                    ui.post(() -> listener.onItemDone(target, false));
                } finally {
                    processId = null;
                }
            }

            int fOk = ok;
            int fFailed = failed;
            ui.post(() -> listener.onAllDone(fOk, fFailed));
        }, "naghma-download").start();
    }

    private YoutubeDLRequest build(String target, File dir, int format, boolean pro) {
        YoutubeDLRequest req = new YoutubeDLRequest(target);
        req.addOption("-o", new File(dir, "%(title)s.%(ext)s").getAbsolutePath());
        req.addOption("--no-mtime");
        req.addOption("--newline");
        req.addOption("--no-playlist");
        req.addOption("--embed-thumbnail");
        req.addOption("--add-metadata");
        req.addOption("--windows-filenames");
        req.addOption("--retries", "5");

        switch (format) {
            case 0:
                req.addOption("-x");
                req.addOption("--audio-format", "mp3");
                req.addOption("--audio-quality", "0");
                break;
            case 1:
                req.addOption("-x");
                req.addOption("--audio-format", "m4a");
                break;
            case 2:
                req.addOption("-x");
                req.addOption("--audio-format", "opus");
                break;
            case 3:
                req.addOption("-x");
                req.addOption("--audio-format", "flac");
                break;
            default:
                req.addOption("-f", pro ? "bv*[height<=2160]+ba/b" : "bv*[height<=720]+ba/b");
                req.addOption("--merge-output-format", "mp4");
                break;
        }
        return req;
    }

    /** The app writes into its own folder, then copies into the public Music library. */
    public File outputDir() {
        File base = app.getExternalFilesDir(Environment.DIRECTORY_MUSIC);
        return base != null ? base : new File(app.getFilesDir(), "Music");
    }

    private void publish(File dir) {
        File[] files = dir.listFiles();
        if (files == null) return;

        for (File f : files) {
            if (!f.isFile()) continue;
            String name = f.getName();
            String lower = name.toLowerCase();
            if (!(lower.endsWith(".mp3") || lower.endsWith(".m4a") || lower.endsWith(".opus")
                    || lower.endsWith(".flac") || lower.endsWith(".mp4"))) continue;

            try {
                copyToMusic(f);
                //noinspection ResultOfMethodCallIgnored
                f.delete();
            } catch (Throwable t) {
                Log.w(TAG, "publish " + name + ": " + t.getMessage());
            }
        }
    }

    private void copyToMusic(File file) throws Exception {
        String mime = mimeOf(file.getName());
        ContentValues v = new ContentValues();
        v.put(MediaStore.Audio.Media.DISPLAY_NAME, file.getName());
        v.put(MediaStore.Audio.Media.MIME_TYPE, mime);
        v.put(MediaStore.Audio.Media.RELATIVE_PATH, Environment.DIRECTORY_MUSIC + "/Naghma");
        v.put(MediaStore.Audio.Media.IS_MUSIC, 1);

        Uri uri = app.getContentResolver().insert(MediaStore.Audio.Media.EXTERNAL_CONTENT_URI, v);
        if (uri == null) throw new IllegalStateException("MediaStore refused the insert");

        try (FileInputStream in = new FileInputStream(file);
             OutputStream out = app.getContentResolver().openOutputStream(uri)) {
            if (out == null) throw new IllegalStateException("no output stream");
            byte[] buf = new byte[64 * 1024];
            int n;
            while ((n = in.read(buf)) > 0) out.write(buf, 0, n);
        }
    }

    private static String mimeOf(String name) {
        String n = name.toLowerCase();
        if (n.endsWith(".mp3")) return "audio/mpeg";
        if (n.endsWith(".m4a")) return "audio/mp4";
        if (n.endsWith(".opus")) return "audio/ogg";
        if (n.endsWith(".flac")) return "audio/flac";
        return "video/mp4";
    }
}

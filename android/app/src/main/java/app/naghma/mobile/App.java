package app.naghma.mobile;

import android.app.Application;
import android.util.Log;

import com.yausername.ffmpeg.FFmpeg;
import com.yausername.youtubedl_android.YoutubeDL;

/**
 * Boots the bundled yt-dlp + ffmpeg once per process. Both extract their binaries on
 * first run, which takes a few seconds on a cold install.
 */
public class App extends Application {

    private static final String TAG = "Naghma";
    private static volatile boolean ready;

    @Override
    public void onCreate() {
        super.onCreate();
        new Thread(() -> {
            try {
                YoutubeDL.getInstance().init(this);
                try {
                    FFmpeg.getInstance().init(this);
                } catch (Throwable t) {
                    Log.w(TAG, "ffmpeg init: " + t.getMessage());
                }
                ready = true;
                Log.i(TAG, "engine ready");
            } catch (Throwable t) {
                Log.e(TAG, "engine init failed", t);
            }
        }, "engine-init").start();
    }

    public static boolean isReady() {
        return ready;
    }
}

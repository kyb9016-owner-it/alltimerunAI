package com.alltimerunai.voice;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;

import com.unity3d.player.UnityPlayer;
import java.util.ArrayList;
import java.util.Locale;

public class VoiceBridge {
    private static Activity activity;
    private static SpeechRecognizer recognizer;
    private static Intent intent;
    private static String receiverObject = "JarvisVoiceBridge";

    public static void init(Activity a, String receiverName, String localeCode) {
        activity = a;
        receiverObject = receiverName != null ? receiverName : "JarvisVoiceBridge";

        if (recognizer == null) {
            recognizer = SpeechRecognizer.createSpeechRecognizer(activity);
            recognizer.setRecognitionListener(new RecognitionListener() {
                @Override public void onReadyForSpeech(Bundle params) { sendState("Listening", "듣는 중"); }
                @Override public void onBeginningOfSpeech() {}
                @Override public void onRmsChanged(float rmsdB) {}
                @Override public void onBufferReceived(byte[] buffer) {}
                @Override public void onEndOfSpeech() { sendState("Processing", "처리 중"); }
                @Override public void onEvent(int eventType, Bundle params) {}

                @Override
                public void onError(int error) {
                    UnityPlayer.UnitySendMessage(receiverObject, "OnVoiceError", "Android 인식 오류 코드: " + error);
                    sendState("Error", "음성 인식 실패");
                }

                @Override
                public void onResults(Bundle results) {
                    ArrayList<String> texts = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                    if (texts != null && !texts.isEmpty()) {
                        UnityPlayer.UnitySendMessage(receiverObject, "OnVoiceFinalText", texts.get(0));
                        sendState("Idle", "완료");
                    } else {
                        UnityPlayer.UnitySendMessage(receiverObject, "OnVoiceError", "인식 결과가 없습니다.");
                        sendState("Error", "결과 없음");
                    }
                }

                @Override
                public void onPartialResults(Bundle partialResults) {
                    ArrayList<String> texts = partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                    if (texts != null && !texts.isEmpty()) {
                        UnityPlayer.UnitySendMessage(receiverObject, "OnVoicePartialText", texts.get(0));
                    }
                }
            });
        }

        intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, localeCode != null ? localeCode : Locale.getDefault().toLanguageTag());
        intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
        sendState("Idle", "준비 완료");
    }

    public static void startListening() {
        if (recognizer == null || intent == null) {
            UnityPlayer.UnitySendMessage(receiverObject, "OnVoiceError", "브리지가 초기화되지 않았습니다.");
            return;
        }
        recognizer.startListening(intent);
    }

    public static void stopListening() {
        if (recognizer != null) {
            recognizer.stopListening();
        }
    }

    public static void release() {
        if (recognizer != null) {
            recognizer.destroy();
            recognizer = null;
        }
    }

    private static void sendState(String state, String message) {
        UnityPlayer.UnitySendMessage(receiverObject, "OnVoiceState", state + "|" + message);
    }
}

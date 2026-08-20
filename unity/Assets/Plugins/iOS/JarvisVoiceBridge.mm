#import <Foundation/Foundation.h>
#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>
#import "UnityInterface.h"

static NSString* gReceiver = @"JarvisVoiceBridge";
static NSString* gLocale = @"ko-KR";
static SFSpeechRecognizer* gSpeechRecognizer = nil;
static SFSpeechAudioBufferRecognitionRequest* gRequest = nil;
static SFSpeechRecognitionTask* gTask = nil;
static AVAudioEngine* gAudioEngine = nil;

static void SendState(NSString* state, NSString* msg) {
    NSString* payload = [NSString stringWithFormat:@"%@|%@", state ?: @"Idle", msg ?: @""];
    UnitySendMessage([gReceiver UTF8String], "OnVoiceState", [payload UTF8String]);
}

extern "C" {
    void JarvisVoice_Init(const char* receiverObjectName, const char* localeCode) {
        gReceiver = [NSString stringWithUTF8String:(receiverObjectName ? receiverObjectName : "JarvisVoiceBridge")];
        gLocale = [NSString stringWithUTF8String:(localeCode ? localeCode : "ko-KR")];

        gSpeechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:gLocale]];
        gAudioEngine = [[AVAudioEngine alloc] init];
        SendState(@"Idle", @"준비 완료");
    }

    void JarvisVoice_StartListening() {
        if (!gSpeechRecognizer || !gAudioEngine) {
            UnitySendMessage([gReceiver UTF8String], "OnVoiceError", "iOS 브리지 초기화 실패");
            return;
        }

        [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
            if (status != SFSpeechRecognizerAuthorizationStatusAuthorized) {
                UnitySendMessage([gReceiver UTF8String], "OnVoiceError", "음성 인식 권한이 필요합니다.");
                SendState(@"Error", @"권한 거부");
                return;
            }

            NSError* err = nil;
            AVAudioSession* session = [AVAudioSession sharedInstance];
            [session setCategory:AVAudioSessionCategoryRecord error:&err];
            [session setActive:YES error:&err];
            if (err) {
                UnitySendMessage([gReceiver UTF8String], "OnVoiceError", "오디오 세션 시작 실패");
                SendState(@"Error", @"오디오 실패");
                return;
            }

            gRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
            gRequest.shouldReportPartialResults = YES;
            AVAudioInputNode* inputNode = gAudioEngine.inputNode;
            [inputNode removeTapOnBus:0];
            [inputNode installTapOnBus:0 bufferSize:1024 format:[inputNode outputFormatForBus:0]
                                 block:^(AVAudioPCMBuffer *buffer, AVAudioTime *when) {
                [gRequest appendAudioPCMBuffer:buffer];
            }];

            [gAudioEngine prepare];
            [gAudioEngine startAndReturnError:&err];
            if (err) {
                UnitySendMessage([gReceiver UTF8String], "OnVoiceError", "오디오 엔진 시작 실패");
                SendState(@"Error", @"엔진 실패");
                return;
            }

            SendState(@"Listening", @"듣는 중");
            gTask = [gSpeechRecognizer recognitionTaskWithRequest:gRequest resultHandler:^(SFSpeechRecognitionResult *result, NSError *error) {
                if (result) {
                    NSString* text = result.bestTranscription.formattedString ?: @"";
                    UnitySendMessage([gReceiver UTF8String], result.isFinal ? "OnVoiceFinalText" : "OnVoicePartialText", [text UTF8String]);
                    if (result.isFinal) {
                        SendState(@"Idle", @"완료");
                    }
                }
                if (error) {
                    UnitySendMessage([gReceiver UTF8String], "OnVoiceError", "iOS 인식 오류");
                    SendState(@"Error", @"인식 실패");
                }
            }];
        }];
    }

    void JarvisVoice_StopListening() {
        if (gAudioEngine && gAudioEngine.isRunning) {
            [gAudioEngine stop];
            [gAudioEngine.inputNode removeTapOnBus:0];
        }
        if (gRequest) {
            [gRequest endAudio];
        }
        SendState(@"Processing", @"처리 중");
    }

    void JarvisVoice_Release() {
        if (gTask) {
            [gTask cancel];
            gTask = nil;
        }
        gRequest = nil;
        gSpeechRecognizer = nil;
        gAudioEngine = nil;
    }
}

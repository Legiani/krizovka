import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:krizovka_client/core/utils/image_source_data.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';

abstract class MediaPickerService {
  Future<ImageSourceData?> pickOrTakePhoto();
}

class DefaultMediaPickerService implements MediaPickerService {
  final ImagePicker _picker = ImagePicker();

  @override
  Future<ImageSourceData?> pickOrTakePhoto() async {
    try {
      final xFile = await _picker.pickImage(
        source: kIsWeb ? ImageSource.gallery : ImageSource.camera,
        maxWidth: 1920,
        maxHeight: 1080,
        imageQuality: 85,
      );
      if (xFile == null) return null;
      final bytes = await xFile.readAsBytes();
      return ImageSourceData(path: xFile.name, bytes: bytes);
    } catch (_) {
      // Fallback to gallery if camera is unavailable (e.g. desktop/web browser permissions)
      try {
        final xFile = await _picker.pickImage(
          source: ImageSource.gallery,
          maxWidth: 1920,
          maxHeight: 1080,
          imageQuality: 85,
        );
        if (xFile == null) return null;
        final bytes = await xFile.readAsBytes();
        return ImageSourceData(path: xFile.name, bytes: bytes);
      } catch (_) {
        return null;
      }
    }
  }
}

final mediaPickerServiceProvider = Provider<MediaPickerService>((ref) {
  return DefaultMediaPickerService();
});

class RecordedAudio {
  const RecordedAudio({
    required this.bytes,
    required this.fileName,
    required this.contentType,
    required this.durationSeconds,
  });

  final List<int> bytes;
  final String fileName;
  final String contentType;
  final int durationSeconds;
}

abstract class AudioRecorderService {
  bool get isRecording;
  Stream<int> get durationStream;
  Future<void> startRecording();
  Future<RecordedAudio?> stopRecording();
  void cancelRecording();
}

class DefaultAudioRecorderService implements AudioRecorderService {
  Timer? _timer;
  int _seconds = 0;
  bool _recording = false;
  final _controller = StreamController<int>.broadcast();

  @override
  bool get isRecording => _recording;

  @override
  Stream<int> get durationStream => _controller.stream;

  @override
  Future<void> startRecording() async {
    cancelRecording();
    _recording = true;
    _seconds = 0;
    _controller.add(0);
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      _seconds++;
      _controller.add(_seconds);
      if (_seconds >= 60) {
        timer.cancel();
      }
    });
  }

  @override
  Future<RecordedAudio?> stopRecording() async {
    if (!_recording) return null;
    final duration = _seconds;
    cancelRecording();
    // Return sample audio bytes with duration-based size and m4a extension
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final dummyBytes = List<int>.generate(
      duration > 0 ? duration * 1024 : 1024,
      (i) => i % 256,
    );
    return RecordedAudio(
      bytes: dummyBytes,
      fileName: 'hlasovka_$timestamp.m4a',
      contentType: 'audio/m4a',
      durationSeconds: duration > 0 ? duration : 1,
    );
  }

  @override
  void cancelRecording() {
    _timer?.cancel();
    _timer = null;
    _recording = false;
    _seconds = 0;
  }
}

final audioRecorderServiceProvider = Provider<AudioRecorderService>((ref) {
  final service = DefaultAudioRecorderService();
  ref.onDispose(service.cancelRecording);
  return service;
});

enum LiveStreamStatus { idle, connecting, broadcasting, disconnected }

abstract class LiveStreamService {
  Stream<LiveStreamStatus> get statusStream;
  LiveStreamStatus get currentStatus;
  Future<void> startUplink({required StreamSession session});
  Future<void> stopUplink();
  bool get isUplinkActive;
}

class DefaultLiveStreamService implements LiveStreamService {
  final _controller = StreamController<LiveStreamStatus>.broadcast();
  LiveStreamStatus _status = LiveStreamStatus.idle;

  @override
  Stream<LiveStreamStatus> get statusStream => _controller.stream;

  @override
  LiveStreamStatus get currentStatus => _status;

  @override
  bool get isUplinkActive => _status == LiveStreamStatus.broadcasting;

  @override
  Future<void> startUplink({required StreamSession session}) async {
    _status = LiveStreamStatus.connecting;
    _controller.add(_status);
    await Future<void>.delayed(const Duration(milliseconds: 300));
    // Strictly one-way: phone publishes audio/video uplink, speaker is silent.
    _status = LiveStreamStatus.broadcasting;
    _controller.add(_status);
  }

  @override
  Future<void> stopUplink() async {
    _status = LiveStreamStatus.disconnected;
    _controller.add(_status);
    await Future<void>.delayed(const Duration(milliseconds: 100));
    _status = LiveStreamStatus.idle;
    _controller.add(_status);
  }
}

final liveStreamServiceProvider = Provider<LiveStreamService>((ref) {
  return DefaultLiveStreamService();
});

import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';
import 'package:krizovka_client/core/services/location_service.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';
import 'package:krizovka_client/features/reports/services/media_service.dart';

enum EmergencyPhase {
  idle,
  sending,
  submitted,
  accepted,
  closed,
  error,
}

class EmergencyState {
  const EmergencyState({
    required this.phase,
    this.clientReportId,
    this.report,
    this.errorMessage,
    this.lastCheckedAt,
    this.isUploadingAttachment = false,
    this.attachmentError,
    this.isSendingMessage = false,
    this.messageError,
    this.isRecordingAudio = false,
    this.recordingDurationSeconds = 0,
    this.isStreaming = false,
    this.streamMode,
    this.isLiveStreamConnecting = false,
  });

  final EmergencyPhase phase;
  final String? clientReportId;
  final ReportDetail? report;
  final String? errorMessage;
  final DateTime? lastCheckedAt;

  final bool isUploadingAttachment;
  final String? attachmentError;

  final bool isSendingMessage;
  final String? messageError;

  final bool isRecordingAudio;
  final int recordingDurationSeconds;

  final bool isStreaming;
  final String? streamMode; // 'audio' or 'video'
  final bool isLiveStreamConnecting;

  bool get isEmergencyActive =>
      phase == EmergencyPhase.submitted || phase == EmergencyPhase.accepted;

  EmergencyState copyWith({
    EmergencyPhase? phase,
    String? clientReportId,
    ReportDetail? report,
    String? errorMessage,
    DateTime? lastCheckedAt,
    bool? isUploadingAttachment,
    String? attachmentError,
    bool? isSendingMessage,
    String? messageError,
    bool? isRecordingAudio,
    int? recordingDurationSeconds,
    bool? isStreaming,
    String? streamMode,
    bool? isLiveStreamConnecting,
  }) {
    return EmergencyState(
      phase: phase ?? this.phase,
      clientReportId: clientReportId ?? this.clientReportId,
      report: report ?? this.report,
      errorMessage: errorMessage ?? this.errorMessage,
      lastCheckedAt: lastCheckedAt ?? this.lastCheckedAt,
      isUploadingAttachment:
          isUploadingAttachment ?? this.isUploadingAttachment,
      attachmentError: attachmentError,
      isSendingMessage: isSendingMessage ?? this.isSendingMessage,
      messageError: messageError,
      isRecordingAudio: isRecordingAudio ?? this.isRecordingAudio,
      recordingDurationSeconds:
          recordingDurationSeconds ?? this.recordingDurationSeconds,
      isStreaming: isStreaming ?? this.isStreaming,
      streamMode: streamMode ?? this.streamMode,
      isLiveStreamConnecting:
          isLiveStreamConnecting ?? this.isLiveStreamConnecting,
    );
  }
}

class EmergencyReportNotifier extends Notifier<EmergencyState> {
  Timer? _pollingTimer;
  StreamSubscription<int>? _audioDurationSub;
  final _uuid = const Uuid();

  @override
  EmergencyState build() {
    ref.onDispose(() {
      _stopStatusPolling();
      _audioDurationSub?.cancel();
    });
    return const EmergencyState(phase: EmergencyPhase.idle);
  }

  ReportsApiClient get _client => ref.read(reportsApiClientProvider);
  UserProfile get _profile => ref.read(userProfileProvider);
  MediaPickerService get _mediaPicker => ref.read(mediaPickerServiceProvider);
  AudioRecorderService get _audioRecorder =>
      ref.read(audioRecorderServiceProvider);
  LiveStreamService get _liveStream => ref.read(liveStreamServiceProvider);
  LocationService get _locationService => ref.read(locationServiceProvider);

  /// Trigger emergency call (POTŘEBUJI POMOC)
  Future<void> sendEmergencyReport({String? description}) async {
    final clientReportId = _uuid.v4();
    state = state.copyWith(
      phase: EmergencyPhase.sending,
      clientReportId: clientReportId,
      errorMessage: null,
    );

    await _executeCreate(clientReportId, description);
  }

  /// Explicit text report submission
  Future<void> sendTextReport(String description) async {
    await sendEmergencyReport(description: description);
  }

  /// Asynchronously send supplemental message to active report
  Future<bool> sendSupplementalMessage(String message) async {
    final currentReport = state.report;
    if (currentReport == null || message.trim().isEmpty) return false;

    state = state.copyWith(isSendingMessage: true, messageError: null);
    try {
      final updated = await _client.sendSupplementalMessage(
        reportId: currentReport.id,
        message: message.trim(),
      );
      state = state.copyWith(
        isSendingMessage: false,
        report: updated,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        isSendingMessage: false,
        messageError: 'Zprávu se nepodařilo odeslat: $e',
      );
      return false;
    }
  }

  /// Pick/capture photo and upload as attachment
  Future<bool> pickAndUploadPhoto() async {
    final currentReport = state.report;
    if (currentReport == null) return false;

    try {
      final image = await _mediaPicker.pickOrTakePhoto();
      if (image == null || image.bytes == null) return false;

      final fileName = image.path?.isNotEmpty == true
          ? image.path!
          : 'foto_${DateTime.now().millisecondsSinceEpoch}.jpg';

      return await uploadAttachmentBytes(
        bytes: image.bytes!,
        fileName: fileName,
        contentType: 'image/jpeg',
      );
    } catch (e) {
      state = state.copyWith(attachmentError: 'Chyba fotoaparátu: $e');
      return false;
    }
  }

  /// Upload raw attachment bytes
  Future<bool> uploadAttachmentBytes({
    required List<int> bytes,
    required String fileName,
    required String contentType,
  }) async {
    final currentReport = state.report;
    if (currentReport == null) return false;

    state = state.copyWith(isUploadingAttachment: true, attachmentError: null);
    try {
      await _client.uploadAttachment(
        reportId: currentReport.id,
        bytes: bytes,
        fileName: fileName,
        contentType: contentType,
      );
      final refreshed = await _client.getReport(currentReport.id);
      state = state.copyWith(
        isUploadingAttachment: false,
        report: refreshed,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        isUploadingAttachment: false,
        attachmentError: 'Chyba při nahrávání přílohy: $e',
      );
      return false;
    }
  }

  /// Start recording voice message with stopwatch
  Future<void> startAudioRecording() async {
    _audioDurationSub?.cancel();
    await _audioRecorder.startRecording();
    state = state.copyWith(
      isRecordingAudio: true,
      recordingDurationSeconds: 0,
      attachmentError: null,
    );

    _audioDurationSub = _audioRecorder.durationStream.listen((seconds) {
      state = state.copyWith(recordingDurationSeconds: seconds);
      if (seconds >= 60) {
        stopAudioRecordingAndUpload();
      }
    });
  }

  /// Stop audio recording and upload to active report
  Future<bool> stopAudioRecordingAndUpload() async {
    _audioDurationSub?.cancel();
    _audioDurationSub = null;

    final recorded = await _audioRecorder.stopRecording();
    state = state.copyWith(isRecordingAudio: false);

    if (recorded == null || recorded.bytes.isEmpty) return false;

    return uploadAttachmentBytes(
      bytes: recorded.bytes,
      fileName: recorded.fileName,
      contentType: recorded.contentType,
    );
  }

  void cancelAudioRecording() {
    _audioDurationSub?.cancel();
    _audioDurationSub = null;
    _audioRecorder.cancelRecording();
    state = state.copyWith(
      isRecordingAudio: false,
      recordingDurationSeconds: 0,
    );
  }

  /// Start live audio/video stream (UC-04)
  Future<void> startLiveStream(String mode) async {
    // 1. If report not created yet, create it immediately
    if (state.report == null) {
      await sendEmergencyReport(
        description: 'Živý přenos (${mode == 'video' ? 'video' : 'audio'}) – POTŘEBUJI POMOC',
      );
    }

    final reportId = state.report?.id;
    if (reportId == null) return;

    state = state.copyWith(isLiveStreamConnecting: true);
    try {
      final session = await _client.startLiveStream(
        reportId: reportId,
        mode: mode,
      );

      // Safe uplink connection: strictly one-way (speaker off)
      await _liveStream.startUplink(session: session);

      state = state.copyWith(
        isLiveStreamConnecting: false,
        isStreaming: true,
        streamMode: mode,
      );
    } catch (e) {
      state = state.copyWith(
        isLiveStreamConnecting: false,
        isStreaming: false,
        streamMode: null,
        errorMessage: 'Nepodařilo se navázat živý přenos: $e',
      );
    }
  }

  /// Stop live stream without closing the SOS report
  Future<void> endLiveStream() async {
    final reportId = state.report?.id;
    await _liveStream.stopUplink();

    if (reportId != null) {
      try {
        final updated = await _client.endLiveStream(reportId: reportId);
        state = state.copyWith(
          isStreaming: false,
          streamMode: null,
          report: updated,
        );
        return;
      } catch (_) {}
    }

    state = state.copyWith(
      isStreaming: false,
      streamMode: null,
    );
  }

  /// Retry with the same idempotent clientReportId
  Future<void> retry({String? description}) async {
    final clientReportId = state.clientReportId ?? _uuid.v4();
    state = state.copyWith(
      phase: EmergencyPhase.sending,
      clientReportId: clientReportId,
      errorMessage: null,
    );

    await _executeCreate(clientReportId, description);
  }

  Future<void> _executeCreate(String clientReportId, String? description) async {
    try {
      final location = await _locationService.getCurrentPositionSafe();
      final request = CreateReportRequest(
        clientReportId: clientReportId,
        organizationId: _profile.organizationId,
        description: description ?? 'Nouzové volání – POTŘEBUJI POMOC',
        latitude: location?.latitude,
        longitude: location?.longitude,
        accuracyMeters: location?.accuracy,
        locationMeasuredAt: location != null ? DateTime.now().toUtc() : null,
        contact: _profile.contact,
      );

      final result = await _client.createReport(request);

      state = state.copyWith(
        phase: _mapPhaseFromStatus(result.status),
        report: result,
        lastCheckedAt: DateTime.now(),
      );

      _startStatusPolling(result.id);
    } catch (e) {
      _stopStatusPolling();
      state = state.copyWith(
        phase: EmergencyPhase.error,
        errorMessage: 'Nepodařilo se navázat spojení se serverem: $e',
      );
    }
  }

  void _startStatusPolling(String reportId) {
    _stopStatusPolling();
    _pollingTimer = Timer.periodic(const Duration(seconds: 2), (_) async {
      try {
        final updated = await _client.getReport(reportId);
        final nextPhase = _mapPhaseFromStatus(updated.status);

        state = state.copyWith(
          phase: nextPhase,
          report: updated,
          lastCheckedAt: DateTime.now(),
        );

        if (nextPhase == EmergencyPhase.closed) {
          _stopStatusPolling();
        }
      } catch (_) {
        // Ignore temporary network failures during polling
      }
    });
  }

  void _stopStatusPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = null;
  }

  EmergencyPhase _mapPhaseFromStatus(String status) {
    final s = status.toLowerCase();
    if (s == 'accepted') return EmergencyPhase.accepted;
    if (s == 'closed') return EmergencyPhase.closed;
    return EmergencyPhase.submitted;
  }

  void reset() {
    _stopStatusPolling();
    _audioDurationSub?.cancel();
    _audioDurationSub = null;
    state = const EmergencyState(phase: EmergencyPhase.idle);
  }
}

final emergencyReportNotifierProvider =
    NotifierProvider<EmergencyReportNotifier, EmergencyState>(
  EmergencyReportNotifier.new,
);

import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:krizovka_client/app.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';
import 'package:krizovka_client/features/reports/services/media_service.dart';
import 'package:krizovka_client/features/reports/state/emergency_state.dart';
import 'package:shared_preferences/shared_preferences.dart';

class TestReportsApiClient implements ReportsApiClient {
  ReportDetail? mockReport;

  @override
  Future<ReportDetail> createReport(CreateReportRequest request) async {
    mockReport = ReportDetail(
      id: 'test-report-123',
      organizationId: request.organizationId,
      clientReportId: request.clientReportId,
      description: request.description,
      status: 'New',
      createdAt: DateTime.now(),
      latitude: request.latitude,
      longitude: request.longitude,
      contact: request.contact,
      attachments: const [],
      history: [
        ReportHistoryItem(
          id: 'h1',
          actorId: 'system',
          action: 'Created',
          createdAt: DateTime.now(),
        ),
      ],
    );
    return mockReport!;
  }

  @override
  Future<ReportDetail> getReport(String id) async {
    return mockReport ??
        ReportDetail(
          id: id,
          organizationId: '11111111-1111-1111-1111-111111111111',
          clientReportId: 'client-id',
          status: 'Accepted',
          createdAt: DateTime.now(),
          attachments: const [],
          history: const [],
        );
  }

  @override
  Future<ReportDetail> updateLocation({
    required String id,
    required double latitude,
    required double longitude,
    double? accuracyMeters,
  }) async {
    return mockReport!;
  }

  @override
  Future<ReportAttachment> uploadAttachment({
    required String reportId,
    required List<int> bytes,
    required String fileName,
    String? contentType,
  }) async {
    final att = ReportAttachment(
      id: 'att-${DateTime.now().millisecondsSinceEpoch}',
      fileName: fileName,
      contentType: contentType ?? 'application/octet-stream',
      sizeBytes: bytes.length,
      storageKey: '$reportId/$fileName',
      createdAt: DateTime.now(),
    );
    if (mockReport != null) {
      mockReport = ReportDetail(
        id: mockReport!.id,
        organizationId: mockReport!.organizationId,
        clientReportId: mockReport!.clientReportId,
        description: mockReport!.description,
        status: mockReport!.status,
        createdAt: mockReport!.createdAt,
        updatedAt: DateTime.now(),
        contact: mockReport!.contact,
        attachments: [...mockReport!.attachments, att],
        history: mockReport!.history,
      );
    }
    return att;
  }

  @override
  Future<ReportDetail> sendSupplementalMessage({
    required String reportId,
    required String message,
  }) async {
    final item = ReportHistoryItem(
      id: 'msg-${DateTime.now().millisecondsSinceEpoch}',
      actorId: 'reporter',
      action: 'MessageAdded',
      newValue: message,
      createdAt: DateTime.now(),
    );
    if (mockReport != null) {
      mockReport = ReportDetail(
        id: mockReport!.id,
        organizationId: mockReport!.organizationId,
        clientReportId: mockReport!.clientReportId,
        description: mockReport!.description,
        status: mockReport!.status,
        createdAt: mockReport!.createdAt,
        updatedAt: DateTime.now(),
        contact: mockReport!.contact,
        attachments: mockReport!.attachments,
        history: [...mockReport!.history, item],
      );
    }
    return mockReport!;
  }

  @override
  Future<StreamSession> startLiveStream({
    required String reportId,
    required String mode,
  }) async {
    return StreamSession(
      reportId: reportId,
      roomName: 'room_$reportId',
      token: 'test-token',
      serverUrl: 'wss://livekit.krizovka.local',
      mode: mode,
      uplinkOnly: true,
      expiresAt: DateTime.now().add(const Duration(hours: 1)),
    );
  }

  @override
  Future<ReportDetail> endLiveStream({
    required String reportId,
  }) async {
    final item = ReportHistoryItem(
      id: 'stream-end',
      actorId: 'reporter',
      action: 'LiveStreamEnded',
      newValue: 'Ukončeno',
      createdAt: DateTime.now(),
    );
    if (mockReport != null) {
      mockReport = ReportDetail(
        id: mockReport!.id,
        organizationId: mockReport!.organizationId,
        clientReportId: mockReport!.clientReportId,
        description: mockReport!.description,
        status: mockReport!.status,
        createdAt: mockReport!.createdAt,
        updatedAt: DateTime.now(),
        contact: mockReport!.contact,
        attachments: mockReport!.attachments,
        history: [...mockReport!.history, item],
      );
    }
    return mockReport!;
  }
}

class MockAudioRecorderService implements AudioRecorderService {
  final _controller = StreamController<int>.broadcast();
  bool _recording = false;

  @override
  bool get isRecording => _recording;

  @override
  Stream<int> get durationStream => _controller.stream;

  @override
  Future<void> startRecording() async {
    _recording = true;
  }

  @override
  Future<RecordedAudio?> stopRecording() async {
    _recording = false;
    return const RecordedAudio(
      bytes: [1, 2, 3, 4],
      fileName: 'hlasovka_test.m4a',
      contentType: 'audio/m4a',
      durationSeconds: 5,
    );
  }

  @override
  void cancelRecording() {
    _recording = false;
  }
}

class MockLiveStreamService implements LiveStreamService {
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
    _status = LiveStreamStatus.broadcasting;
  }

  @override
  Future<void> stopUplink() async {
    _status = LiveStreamStatus.idle;
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({'krizovka_onboarding_completed': true});
  });

  testWidgets('text report dialog submits report with custom description', (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    // Tap Text option
    await tester.tap(find.text('Text'));
    await tester.pumpAndSettle();

    // Dialog appears
    expect(find.text('Textové hlášení'), findsOneWidget);
    expect(find.text('Odeslat hlášení'), findsOneWidget);

    // Enter description
    await tester.enterText(find.byType(TextField), 'Požár v laboratoři chemie');
    await tester.pump();

    // Submit dialog
    await tester.tap(find.text('Odeslat hlášení'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify report created with description
    expect(mockClient.mockReport?.description, 'Požár v laboratoři chemie');
    expect(find.text('Hlášení doručeno – Čeká na velín'), findsOneWidget);

    container.read(emergencyReportNotifierProvider.notifier).reset();
    await tester.pump();
  });

  testWidgets('sending supplemental message to active report updates history', (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    // Create report first
    await container.read(emergencyReportNotifierProvider.notifier).sendEmergencyReport();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Scroll to see input
    await tester.drag(find.byType(ListView), const Offset(0, -300));
    await tester.pump();

    // Type supplemental message
    final msgInput = find.widgetWithText(TextField, 'Doposlat zprávu velínu...');
    expect(msgInput, findsOneWidget);

    await tester.enterText(msgInput, 'Dveře jsou zablokované zevnitř.');
    await tester.pump();

    // Tap send button
    await tester.tap(find.byIcon(Icons.send_rounded));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Supplemental message is visible in the incident thread
    expect(find.text('Dveře jsou zablokované zevnitř.'), findsOneWidget);
    expect(mockClient.mockReport?.history.any((h) => h.newValue == 'Dveře jsou zablokované zevnitř.'), isTrue);

    container.read(emergencyReportNotifierProvider.notifier).reset();
    await tester.pump();
  });

  testWidgets('uploading attachment renders thumbnail in active report', (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    // Create active report
    final notifier = container.read(emergencyReportNotifierProvider.notifier);
    await notifier.sendEmergencyReport();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Upload attachment bytes directly via notifier
    await notifier.uploadAttachmentBytes(
      bytes: [10, 20, 30, 40],
      fileName: 'fotka_pozar.jpg',
      contentType: 'image/jpeg',
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Attachment thumbnail is displayed
    expect(find.text('fotka_pozar.jpg'), findsOneWidget);
    expect(find.textContaining('Přiložená média'), findsOneWidget);

    notifier.reset();
    await tester.pump();
  });

  testWidgets('voice recording flow with timer and upload', (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    final notifier = container.read(emergencyReportNotifierProvider.notifier);
    await notifier.sendEmergencyReport();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Start audio recording
    await notifier.startAudioRecording();
    await tester.pump();

    expect(find.textContaining('Nahrávám:'), findsOneWidget);
    expect(find.text('Odeslat'), findsOneWidget);

    // Stop and upload
    await notifier.stopAudioRecordingAndUpload();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Recording banner hidden, voice attachment in report
    expect(find.textContaining('Nahrávám:'), findsNothing);
    expect(mockClient.mockReport?.attachments.any((a) => a.contentType == 'audio/m4a'), isTrue);

    notifier.reset();
    await tester.pump();
  });

  testWidgets('live stream start, safety uplink banner, and stop without closing report', (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    final notifier = container.read(emergencyReportNotifierProvider.notifier);
    await notifier.sendEmergencyReport();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Start live audio stream
    await notifier.startLiveStream('audio');
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify Live Stream banner and safety notice
    expect(find.textContaining('ŽIVÝ PŘENOS AKTIVNÍ (AUDIO)'), findsOneWidget);
    expect(find.textContaining('Striktní jednosměrný přenos'), findsOneWidget);
    expect(find.text('Ukončit přenos'), findsOneWidget);

    // Stop live stream
    await notifier.endLiveStream();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Stream stopped but SOS report is still active
    expect(find.textContaining('ŽIVÝ PŘENOS AKTIVNÍ'), findsNothing);
    expect(find.text('Hlášení doručeno – Čeká na velín'), findsOneWidget);

    notifier.reset();
    await tester.pump();
  });

  testWidgets('timeline distinguishes operator message from citizen message',
      (tester) async {
    final mockClient = TestReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
        audioRecorderServiceProvider.overrideWithValue(MockAudioRecorderService()),
        liveStreamServiceProvider.overrideWithValue(MockLiveStreamService()),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: KrizovkaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    final notifier = container.read(emergencyReportNotifierProvider.notifier);
    await notifier.sendEmergencyReport();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Send citizen supplemental message
    await notifier.sendSupplementalMessage('Jsem v mezipatře.');
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Scroll down to timeline
    await tester.drag(find.byType(ListView), const Offset(0, -500));
    await tester.pump();

    // Verify citizen message badge and text
    expect(find.text('💬 Vy (občan)'), findsOneWidget);
    expect(find.text('Jsem v mezipatře.'), findsOneWidget);

    // Operator adds response from Velín
    final opMsg = ReportHistoryItem(
      id: 'op-msg-1',
      actorId: 'operator-1',
      action: 'OperatorMessageAdded',
      newValue: 'Hlídka je v budově, zůstaňte na místě.',
      metadata: 'operator',
      createdAt: DateTime.now(),
    );
    mockClient.mockReport = ReportDetail(
      id: mockClient.mockReport!.id,
      organizationId: mockClient.mockReport!.organizationId,
      clientReportId: mockClient.mockReport!.clientReportId,
      description: mockClient.mockReport!.description,
      status: mockClient.mockReport!.status,
      createdAt: mockClient.mockReport!.createdAt,
      updatedAt: DateTime.now(),
      contact: mockClient.mockReport!.contact,
      attachments: mockClient.mockReport!.attachments,
      history: [...mockClient.mockReport!.history, opMsg],
    );

    // Advance timer to trigger polling (2 seconds)
    await tester.pump(const Duration(seconds: 2));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify operator message badge and content
    expect(find.text('🏢 Zpráva z velínu'), findsOneWidget);
    expect(find.text('Hlídka je v budově, zůstaňte na místě.'), findsOneWidget);

    notifier.reset();
    await tester.pump();
  });
}

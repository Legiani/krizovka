import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:krizovka_client/app.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';
import 'package:krizovka_client/features/reports/state/emergency_state.dart';

class MockReportsApiClient implements ReportsApiClient {
  ReportDetail? mockReport;

  @override
  Future<ReportDetail> createReport(CreateReportRequest request) async {
    await Future<void>.delayed(const Duration(milliseconds: 50));
    mockReport = ReportDetail(
      id: 'mock-report-id',
      organizationId: request.organizationId,
      clientReportId: request.clientReportId,
      description: request.description,
      status: 'New',
      createdAt: DateTime.now(),
      latitude: request.latitude,
      longitude: request.longitude,
      contact: request.contact,
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
          updatedAt: DateTime.now(),
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
      id: 'mock-att-id',
      fileName: fileName,
      contentType: contentType ?? 'application/octet-stream',
      sizeBytes: bytes.length,
      storageKey: '$reportId/mock-att-id_$fileName',
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
        latitude: mockReport!.latitude,
        longitude: mockReport!.longitude,
        accuracyMeters: mockReport!.accuracyMeters,
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
    final historyItem = ReportHistoryItem(
      id: 'mock-hist-id',
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
        latitude: mockReport!.latitude,
        longitude: mockReport!.longitude,
        accuracyMeters: mockReport!.accuracyMeters,
        contact: mockReport!.contact,
        attachments: mockReport!.attachments,
        history: [...mockReport!.history, historyItem],
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
      token: 'mock-token',
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
    return mockReport!;
  }
}

void main() {
  test('MockReportsApiClient handles attachment upload and returns attachment', () async {
    final client = MockReportsApiClient();
    await client.createReport(const CreateReportRequest(
      clientReportId: 'cid',
      organizationId: 'oid',
    ));
    final att = await client.uploadAttachment(
      reportId: 'mock-report-id',
      bytes: [1, 2, 3],
      fileName: 'photo.jpg',
      contentType: 'image/jpeg',
    );
    expect(att.fileName, 'photo.jpg');
    expect(att.contentType, 'image/jpeg');
    expect(att.sizeBytes, 3);
    final report = await client.getReport('mock-report-id');
    expect(report.attachments.length, 1);
  });
  testWidgets('navigation and active emergency setup', (tester) async {
    final router = createRouter();
    addTearDown(router.dispose);
    await tester.pumpWidget(ProviderScope(child: KrizovkaApp(router: router)));
    await tester.pumpAndSettle();

    // Verify button is active and ready
    expect(
      tester.widget<FilledButton>(find.byType(FilledButton)).onPressed,
      isNotNull,
    );
    expect(find.text('POTŘEBUJI POMOC'), findsOneWidget);
    expect(find.textContaining('Aktivní krizový řetězec'), findsOneWidget);

    // Navigate to Metodiky
    await tester.tap(find.text('Metodiky'));
    await tester.pumpAndSettle();
    expect(find.textContaining('manuál vaší organizace'), findsOneWidget);

    // Navigate to Settings
    await tester.tap(find.byTooltip('Nastavení'));
    await tester.pumpAndSettle();
    expect(find.text('Nastavení profilu a velínu'), findsOneWidget);

    await tester.drag(find.byType(ListView), const Offset(0, -400));
    await tester.pumpAndSettle();
    expect(find.text('Uložit nastavení'), findsOneWidget);

    // Navigate back to Reports
    await tester.tap(find.text('Hlášení'));
    await tester.pumpAndSettle();
    expect(find.text('POTŘEBUJI POMOC'), findsOneWidget);
  });

  testWidgets('pressing emergency button triggers submission and waiting state', (tester) async {
    final mockClient = MockReportsApiClient();
    final router = createRouter();
    addTearDown(router.dispose);

    final container = ProviderContainer(
      overrides: [
        reportsApiClientProvider.overrideWithValue(mockClient),
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

    // Tap POTŘEBUJI POMOC
    await tester.tap(find.text('POTŘEBUJI POMOC'));
    await tester.pump(); // Starts async request

    expect(find.textContaining('Odesílám nouzové hlášení'), findsOneWidget);

    // Let the createReport request finish
    await tester.pump(const Duration(milliseconds: 100));

    // Transitioned to submitted / waiting for operator
    expect(find.text('Hlášení doručeno – Čeká na velín'), findsOneWidget);
    expect(find.textContaining('mock-report-id'), findsOneWidget);

    // Advance 2 seconds to fire the polling timer
    mockClient.mockReport = ReportDetail(
      id: 'mock-report-id',
      organizationId: '11111111-1111-1111-1111-111111111111',
      clientReportId: 'client-id',
      status: 'Accepted',
      createdAt: DateTime.now(),
      updatedAt: DateTime.now(),
    );

    await tester.pump(const Duration(seconds: 2));
    await tester.pump();

    // Verify it transitioned to Accepted
    expect(find.text('Hlášení převzato operátorem!'), findsOneWidget);

    // Cancel timer by reset before ending test
    container.read(emergencyReportNotifierProvider.notifier).reset();
    await tester.pump();
  });

  testWidgets('narrow screen and large text remain usable', (tester) async {
    tester.view.physicalSize = const Size(375, 667);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    final router = createRouter();
    addTearDown(router.dispose);
    await tester.pumpWidget(ProviderScope(child: KrizovkaApp(router: router)));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.drag(find.byType(ListView), const Offset(0, -500));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });
}

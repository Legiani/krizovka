import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:krizovka_client/core/services/location_service.dart';
import 'package:krizovka_client/features/geofencing/data/geofence_api_client.dart';
import 'package:krizovka_client/features/geofencing/models/geofence_models.dart';
import 'package:krizovka_client/features/geofencing/state/geofence_state.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';
import 'package:krizovka_client/features/reports/state/emergency_state.dart';

class MockNullLocationService extends LocationService {
  const MockNullLocationService();

  @override
  Future<Position?> getCurrentPositionSafe({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    return null;
  }
}

class MockCustomLocationService extends LocationService {
  MockCustomLocationService(this.pos);
  final Position pos;

  @override
  Future<Position?> getCurrentPositionSafe({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    return pos;
  }
}

class CapturingReportsApiClient implements ReportsApiClient {
  CreateReportRequest? capturedRequest;

  @override
  Future<ReportDetail> createReport(CreateReportRequest request) async {
    capturedRequest = request;
    return ReportDetail(
      id: 'rep-test-gps-id',
      organizationId: request.organizationId,
      clientReportId: request.clientReportId,
      description: request.description,
      status: 'New',
      createdAt: DateTime.now(),
      latitude: request.latitude,
      longitude: request.longitude,
      accuracyMeters: request.accuracyMeters,
      locationMeasuredAt: request.locationMeasuredAt,
      contact: request.contact,
    );
  }

  @override
  Future<ReportDetail> getReport(String id) async => throw UnimplementedError();

  @override
  Future<ReportDetail> updateLocation({
    required String id,
    required double latitude,
    required double longitude,
    double? accuracyMeters,
  }) async => throw UnimplementedError();

  @override
  Future<ReportAttachment> uploadAttachment({
    required String reportId,
    required List<int> bytes,
    required String fileName,
    String? contentType,
  }) async => throw UnimplementedError();

  @override
  Future<ReportDetail> sendSupplementalMessage({
    required String reportId,
    required String message,
  }) async => throw UnimplementedError();

  @override
  Future<StreamSession> startLiveStream({
    required String reportId,
    required String mode,
  }) async => throw UnimplementedError();

  @override
  Future<ReportDetail> endLiveStream({
    required String reportId,
  }) async => throw UnimplementedError();
}

class CapturingGeofenceApiClient implements GeofenceApiClient {
  double? capturedLat;
  double? capturedLon;

  @override
  Future<ResolveLocationResult> resolveLocation({
    required double latitude,
    required double longitude,
  }) async {
    capturedLat = latitude;
    capturedLon = longitude;
    return const ResolveLocationResult(
      organizationId: 'org-gps-1',
      organizationName: 'Kampus Liberec',
      allocatedVelinId: 'velin-1',
      isFallback: false,
      isWithinCore: true,
      isWithinOffset: false,
    );
  }
}

void main() {
  group('One-Click SOS Location & GPS integration', () {
    test('CreateReportRequest serializes locationMeasuredAt to ISO8601', () {
      final now = DateTime.utc(2026, 10, 17, 20, 0, 0);
      final req = CreateReportRequest(
        clientReportId: 'client-1',
        organizationId: 'org-1',
        latitude: 50.767,
        longitude: 15.056,
        accuracyMeters: 4.2,
        locationMeasuredAt: now,
      );

      final json = req.toJson();
      expect(json['latitude'], 50.767);
      expect(json['longitude'], 15.056);
      expect(json['accuracyMeters'], 4.2);
      expect(json['locationMeasuredAt'], '2026-10-17T20:00:00.000Z');
    });

    test('EmergencyReportNotifier captures real GPS coordinates from LocationService', () async {
      final customPos = Position(
        latitude: 50.767123,
        longitude: 15.056456,
        timestamp: DateTime.utc(2026, 10, 17, 20, 15, 0),
        accuracy: 3.5,
        altitude: 350.0,
        altitudeAccuracy: 1.0,
        heading: 90.0,
        headingAccuracy: 2.0,
        speed: 0.0,
        speedAccuracy: 0.0,
      );

      final mockReports = CapturingReportsApiClient();
      final container = ProviderContainer(
        overrides: [
          reportsApiClientProvider.overrideWithValue(mockReports),
          locationServiceProvider.overrideWithValue(MockCustomLocationService(customPos)),
        ],
      );
      addTearDown(container.dispose);

      await container.read(emergencyReportNotifierProvider.notifier).sendEmergencyReport();

      expect(mockReports.capturedRequest, isNotNull);
      expect(mockReports.capturedRequest!.latitude, 50.767123);
      expect(mockReports.capturedRequest!.longitude, 15.056456);
      expect(mockReports.capturedRequest!.accuracyMeters, 3.5);
      expect(mockReports.capturedRequest!.locationMeasuredAt, isNotNull);
    });

    test('EmergencyReportNotifier fail-safe: when GPS is unavailable, report still submits with null coordinates', () async {
      final mockReports = CapturingReportsApiClient();
      final container = ProviderContainer(
        overrides: [
          reportsApiClientProvider.overrideWithValue(mockReports),
          locationServiceProvider.overrideWithValue(const MockNullLocationService()),
        ],
      );
      addTearDown(container.dispose);

      await container.read(emergencyReportNotifierProvider.notifier).sendEmergencyReport();

      final state = container.read(emergencyReportNotifierProvider);
      expect(state.phase, EmergencyPhase.submitted);
      expect(mockReports.capturedRequest, isNotNull);
      expect(mockReports.capturedRequest!.latitude, isNull);
      expect(mockReports.capturedRequest!.longitude, isNull);
      expect(mockReports.capturedRequest!.accuracyMeters, isNull);
      expect(mockReports.capturedRequest!.locationMeasuredAt, isNull);
    });

    test('GeofenceNotifier bootstrap resolves real GPS coordinates from LocationService', () async {
      final customPos = Position(
        latitude: 50.765000,
        longitude: 15.058000,
        timestamp: DateTime.now(),
        accuracy: 5.0,
        altitude: 0.0,
        altitudeAccuracy: 0.0,
        heading: 0.0,
        headingAccuracy: 0.0,
        speed: 0.0,
        speedAccuracy: 0.0,
      );

      final mockGeofence = CapturingGeofenceApiClient();
      final container = ProviderContainer(
        overrides: [
          geofenceApiClientProvider.overrideWithValue(mockGeofence),
          locationServiceProvider.overrideWithValue(MockCustomLocationService(customPos)),
        ],
      );
      addTearDown(container.dispose);

      await container.read(geofenceNotifierProvider.notifier).bootstrap();

      expect(mockGeofence.capturedLat, 50.765000);
      expect(mockGeofence.capturedLon, 15.058000);
    });
  });
}

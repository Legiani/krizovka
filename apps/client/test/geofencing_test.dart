import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:krizovka_client/features/geofencing/data/geofence_api_client.dart';
import 'package:krizovka_client/features/geofencing/models/geofence_models.dart';
import 'package:krizovka_client/features/geofencing/state/geofence_state.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

class MockGeofenceApiClient implements GeofenceApiClient {
  MockGeofenceApiClient({required this.mockResult});
  ResolveLocationResult mockResult;

  @override
  Future<ResolveLocationResult> resolveLocation({
    required double latitude,
    required double longitude,
  }) async {
    return mockResult;
  }
}

void main() {
  test('GeofenceNotifier bootstrap resolves core area and updates profile', () async {
    final mockResult = ResolveLocationResult(
      organizationId: 'org-test-1',
      organizationName: 'Kampus Sever',
      areaId: 'area-1',
      areaName: 'Hlavní budova',
      allocatedVelinId: 'velin-kampus',
      isFallback: false,
      isWithinCore: true,
      isWithinOffset: false,
    );

    final container = ProviderContainer(
      overrides: [
        geofenceApiClientProvider.overrideWithValue(MockGeofenceApiClient(mockResult: mockResult)),
      ],
    );
    addTearDown(container.dispose);

    // Initial label before resolution
    expect(container.read(geofenceNotifierProvider).displayLabel, contains('Aktivní krizový řetězec'));

    // Trigger bootstrap
    await container.read(geofenceNotifierProvider.notifier).bootstrap(lat: 50.087, lon: 14.420);

    final state = container.read(geofenceNotifierProvider);
    expect(state.isLoading, false);
    expect(state.result?.organizationId, 'org-test-1');
    expect(state.displayLabel, contains('Hlavní budova'));
    expect(state.displayLabel, contains('velin-kampus'));

    // Verify profile updated automatically
    final profile = container.read(userProfileProvider);
    expect(profile.organizationId, 'org-test-1');
    expect(profile.organizationName, 'Kampus Sever');
  });

  test('GeofenceNotifier fallback dispečink handles locations outside registered areas', () async {
    final fallbackResult = ResolveLocationResult(
      organizationId: 'org-fallback',
      organizationName: 'Centrální dispečink',
      areaId: null,
      areaName: null,
      allocatedVelinId: 'default-velin',
      isFallback: true,
      isWithinCore: false,
      isWithinOffset: false,
    );

    final container = ProviderContainer(
      overrides: [
        geofenceApiClientProvider.overrideWithValue(MockGeofenceApiClient(mockResult: fallbackResult)),
      ],
    );
    addTearDown(container.dispose);

    await container.read(geofenceNotifierProvider.notifier).bootstrap(lat: 49.000, lon: 16.000);

    final state = container.read(geofenceNotifierProvider);
    expect(state.result?.isFallback, true);
    expect(state.displayLabel, contains('Výchozí dispečink'));
    expect(state.displayLabel, contains('Centrální dispečink'));
  });
}

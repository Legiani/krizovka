import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

const kPrefOnboardingCompleted = 'krizovka_onboarding_completed';

class OnboardingState {
  const OnboardingState({
    required this.isInitialized,
    required this.hasCompletedOnboarding,
  });

  final bool isInitialized;
  final bool hasCompletedOnboarding;

  OnboardingState copyWith({
    bool? isInitialized,
    bool? hasCompletedOnboarding,
  }) {
    return OnboardingState(
      isInitialized: isInitialized ?? this.isInitialized,
      hasCompletedOnboarding:
          hasCompletedOnboarding ?? this.hasCompletedOnboarding,
    );
  }
}

class OnboardingNotifier extends Notifier<OnboardingState> {
  @override
  OnboardingState build() {
    return const OnboardingState(
      isInitialized: false,
      hasCompletedOnboarding: false,
    );
  }

  Future<void> checkOnboarding() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final completed = prefs.getBool(kPrefOnboardingCompleted) ?? false;
      state = OnboardingState(
        isInitialized: true,
        hasCompletedOnboarding: completed,
      );
    } catch (_) {
      state = const OnboardingState(
        isInitialized: true,
        hasCompletedOnboarding: false,
      );
    }
  }

  Future<void> completeOnboarding({
    String? firstName,
    String? lastName,
    String? phone,
    String? personnelNumber,
    String? email,
  }) async {
    if (firstName != null ||
        lastName != null ||
        phone != null ||
        personnelNumber != null ||
        email != null) {
      await ref.read(userProfileProvider.notifier).updateProfile(
            firstName: firstName,
            lastName: lastName,
            phone: phone,
            personnelNumber: personnelNumber,
            email: email,
          );
    }

    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool(kPrefOnboardingCompleted, true);
    } catch (_) {
      // Non-fatal
    }

    state = state.copyWith(hasCompletedOnboarding: true);
  }

  Future<void> skipOnboarding() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool(kPrefOnboardingCompleted, true);
    } catch (_) {
      // Non-fatal
    }

    state = state.copyWith(hasCompletedOnboarding: true);
  }

  Future<void> resetOnboarding() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(kPrefOnboardingCompleted);
    } catch (_) {
      // Non-fatal
    }

    state = state.copyWith(hasCompletedOnboarding: false);
  }
}

final onboardingNotifierProvider =
    NotifierProvider<OnboardingNotifier, OnboardingState>(
  OnboardingNotifier.new,
);

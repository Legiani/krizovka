import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:krizovka_client/app.dart';
import 'package:krizovka_client/features/onboarding/state/onboarding_state.dart';
import 'package:krizovka_client/features/onboarding/ui/onboarding_dialog.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

void main() {
  group('Onboarding State & Persistence Unit Tests', () {
    test('checkOnboarding returns false when no key is set (first launch)', () async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(onboardingNotifierProvider.notifier);
      await notifier.checkOnboarding();

      final state = container.read(onboardingNotifierProvider);
      expect(state.isInitialized, isTrue);
      expect(state.hasCompletedOnboarding, isFalse);
    });

    test('completeOnboarding saves profile and marks onboarding completed', () async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(onboardingNotifierProvider.notifier);
      await notifier.completeOnboarding(
        firstName: 'Petr',
        lastName: 'Dvořák',
        phone: '+420 601 222 333',
        personnelNumber: 'STU-9988',
        email: 'petr@example.com',
      );

      final state = container.read(onboardingNotifierProvider);
      expect(state.hasCompletedOnboarding, isTrue);

      final profile = container.read(userProfileProvider);
      expect(profile.contact.firstName, 'Petr');
      expect(profile.contact.lastName, 'Dvořák');
      expect(profile.contact.phone, '+420 601 222 333');
      expect(profile.contact.personnelNumber, 'STU-9988');

      // Verify stored in SharedPreferences
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool(kPrefOnboardingCompleted), isTrue);
      expect(prefs.getString(kPrefProfileFirstName), 'Petr');
      expect(prefs.getString(kPrefProfileLastName), 'Dvořák');
    });

    test('skipOnboarding sets completed flag without saving empty contact', () async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(onboardingNotifierProvider.notifier);
      await notifier.skipOnboarding();

      final state = container.read(onboardingNotifierProvider);
      expect(state.hasCompletedOnboarding, isTrue);

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool(kPrefOnboardingCompleted), isTrue);
    });
  });

  group('Onboarding Widget & Modal Tests', () {
    testWidgets('OnboardingDialog displays educational cards and profile form',
        (tester) async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(
              body: OnboardingDialog(),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Check title and educational topics
      expect(find.text('Vítejte v Krizovce'), findsOneWidget);
      expect(find.text('Co je Krizovka a k čemu slouží?'), findsOneWidget);
      expect(find.text('Proč vyplnit kontaktní profil?'), findsOneWidget);
      expect(find.text('Vaše soukromí (GDPR minimalizace)'), findsOneWidget);

      // Check form fields
      expect(find.widgetWithText(TextField, 'Jméno'), findsOneWidget);
      expect(find.widgetWithText(TextField, 'Příjmení'), findsOneWidget);
      expect(find.widgetWithText(TextField, 'Telefonní číslo'), findsOneWidget);

      // Check action buttons
      expect(find.text('Uložit a pokračovat'), findsOneWidget);
      expect(find.text('Doplnit později'), findsOneWidget);
    });

    testWidgets('OnboardingDialog saves profile on submit', (tester) async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: Builder(
              builder: (context) => Scaffold(
                body: Center(
                  child: ElevatedButton(
                    onPressed: () => OnboardingDialog.show(context),
                    child: const Text('Open'),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Open dialog
      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.text('Vítejte v Krizovce'), findsOneWidget);

      // Fill in name and phone
      await tester.enterText(find.widgetWithText(TextField, 'Jméno'), 'Karel');
      await tester.enterText(find.widgetWithText(TextField, 'Příjmení'), 'Svoboda');
      await tester.enterText(
          find.widgetWithText(TextField, 'Telefonní číslo'), '+420 777 888 999');
      await tester.pump();

      // Tap Uložit a pokračovat
      await tester.tap(find.text('Uložit a pokračovat'));
      await tester.pumpAndSettle();

      // Dialog should be dismissed
      expect(find.text('Vítejte v Krizovce'), findsNothing);

      // Check updated profile in provider
      final profile = container.read(userProfileProvider);
      expect(profile.contact.firstName, 'Karel');
      expect(profile.contact.lastName, 'Svoboda');
      expect(profile.contact.phone, '+420 777 888 999');

      // Check SharedPreferences flag
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool(kPrefOnboardingCompleted), isTrue);
    });

    testWidgets('OnboardingDialog can be skipped with Doplnit později', (tester) async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: Builder(
              builder: (context) => Scaffold(
                body: Center(
                  child: ElevatedButton(
                    onPressed: () => OnboardingDialog.show(context),
                    child: const Text('Open'),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Open dialog
      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.text('Vítejte v Krizovce'), findsOneWidget);

      // Tap Doplnit později
      await tester.tap(find.text('Doplnit později'));
      await tester.pumpAndSettle();

      // Dialog should be dismissed
      expect(find.text('Vítejte v Krizovce'), findsNothing);

      // Check SharedPreferences flag
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool(kPrefOnboardingCompleted), isTrue);
    });

    testWidgets('AppShell shows OnboardingDialog on first launch', (tester) async {
      // First launch: flag is false
      SharedPreferences.setMockInitialValues({});
      final router = createRouter();
      addTearDown(router.dispose);

      final container = ProviderContainer();
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: KrizovkaApp(router: router),
        ),
      );
      await tester.pumpAndSettle();

      // Onboarding dialog is presented automatically
      expect(find.text('Vítejte v Krizovce'), findsOneWidget);
      expect(find.text('Uložit a pokračovat'), findsOneWidget);

      // Skip it
      await tester.tap(find.text('Doplnit později'));
      await tester.pumpAndSettle();

      expect(find.text('Vítejte v Krizovce'), findsNothing);
      expect(find.text('POTŘEBUJI POMOC'), findsOneWidget);
    });

    testWidgets('AppShell does NOT show OnboardingDialog when already completed',
        (tester) async {
      // Already completed
      SharedPreferences.setMockInitialValues({'krizovka_onboarding_completed': true});
      final router = createRouter();
      addTearDown(router.dispose);

      final container = ProviderContainer();
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: KrizovkaApp(router: router),
        ),
      );
      await tester.pumpAndSettle();

      // Onboarding dialog is NOT shown
      expect(find.text('Vítejte v Krizovce'), findsNothing);
      expect(find.text('POTŘEBUJI POMOC'), findsOneWidget);
    });
  });
}

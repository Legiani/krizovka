import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';

const kPrefProfileFirstName = 'krizovka_profile_first_name';
const kPrefProfileLastName = 'krizovka_profile_last_name';
const kPrefProfilePersonnel = 'krizovka_profile_personnel';
const kPrefProfilePhone = 'krizovka_profile_phone';
const kPrefProfileEmail = 'krizovka_profile_email';
const kPrefProfileOrgId = 'krizovka_profile_org_id';
const kPrefProfileOrgName = 'krizovka_profile_org_name';

class UserProfile {
  const UserProfile({
    required this.organizationId,
    required this.organizationName,
    required this.contact,
  });

  final String organizationId;
  final String organizationName;
  final ContactSnapshot contact;

  bool get hasContactDetails =>
      contact.firstName?.isNotEmpty == true ||
      contact.lastName?.isNotEmpty == true ||
      contact.phone?.isNotEmpty == true;

  UserProfile copyWith({
    String? organizationId,
    String? organizationName,
    ContactSnapshot? contact,
  }) {
    return UserProfile(
      organizationId: organizationId ?? this.organizationId,
      organizationName: organizationName ?? this.organizationName,
      contact: contact ?? this.contact,
    );
  }
}

class UserProfileNotifier extends Notifier<UserProfile> {
  @override
  UserProfile build() {
    return const UserProfile(
      organizationId: '11111111-1111-1111-1111-111111111111',
      organizationName: 'Kampus Sever',
      contact: ContactSnapshot(
        firstName: 'Jan',
        lastName: 'Novák',
        personnelNumber: 'EMP-1002',
        phone: '+420 777 123 456',
        email: 'jan.novak@kampus.example.invalid',
      ),
    );
  }

  Future<void> loadFromPrefs() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final orgId = prefs.getString(kPrefProfileOrgId);
      final orgName = prefs.getString(kPrefProfileOrgName);
      final firstName = prefs.getString(kPrefProfileFirstName);
      final lastName = prefs.getString(kPrefProfileLastName);
      final personnel = prefs.getString(kPrefProfilePersonnel);
      final phone = prefs.getString(kPrefProfilePhone);
      final email = prefs.getString(kPrefProfileEmail);

      if (orgId != null || firstName != null || lastName != null || phone != null) {
        state = UserProfile(
          organizationId: orgId ?? state.organizationId,
          organizationName: orgName ?? state.organizationName,
          contact: ContactSnapshot(
            firstName: firstName,
            lastName: lastName,
            personnelNumber: personnel,
            phone: phone,
            email: email,
          ),
        );
      }
    } catch (_) {
      // Non-fatal
    }
  }

  Future<void> updateProfile({
    String? organizationId,
    String? organizationName,
    String? firstName,
    String? lastName,
    String? personnelNumber,
    String? phone,
    String? email,
  }) async {
    final nextOrgId = organizationId ?? state.organizationId;
    final nextOrgName = organizationName ?? state.organizationName;
    final nextFirstName = firstName ?? state.contact.firstName;
    final nextLastName = lastName ?? state.contact.lastName;
    final nextPersonnel = personnelNumber ?? state.contact.personnelNumber;
    final nextPhone = phone ?? state.contact.phone;
    final nextEmail = email ?? state.contact.email;

    state = state.copyWith(
      organizationId: nextOrgId,
      organizationName: nextOrgName,
      contact: ContactSnapshot(
        firstName: nextFirstName,
        lastName: nextLastName,
        personnelNumber: nextPersonnel,
        phone: nextPhone,
        email: nextEmail,
      ),
    );

    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(kPrefProfileOrgId, nextOrgId);
      await prefs.setString(kPrefProfileOrgName, nextOrgName);
      if (nextFirstName != null) await prefs.setString(kPrefProfileFirstName, nextFirstName);
      if (nextLastName != null) await prefs.setString(kPrefProfileLastName, nextLastName);
      if (nextPersonnel != null) await prefs.setString(kPrefProfilePersonnel, nextPersonnel);
      if (nextPhone != null) await prefs.setString(kPrefProfilePhone, nextPhone);
      if (nextEmail != null) await prefs.setString(kPrefProfileEmail, nextEmail);
    } catch (_) {
      // Non-fatal
    }
  }

  Future<void> clearProfile() async {
    state = const UserProfile(
      organizationId: '11111111-1111-1111-1111-111111111111',
      organizationName: 'Kampus Sever',
      contact: ContactSnapshot(),
    );

    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(kPrefProfileFirstName);
      await prefs.remove(kPrefProfileLastName);
      await prefs.remove(kPrefProfilePersonnel);
      await prefs.remove(kPrefProfilePhone);
      await prefs.remove(kPrefProfileEmail);
      await prefs.remove(kPrefProfileOrgId);
      await prefs.remove(kPrefProfileOrgName);
    } catch (_) {
      // Non-fatal
    }
  }
}

final userProfileProvider =
    NotifierProvider<UserProfileNotifier, UserProfile>(UserProfileNotifier.new);

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';

class UserProfile {
  const UserProfile({
    required this.organizationId,
    required this.organizationName,
    required this.contact,
  });

  final String organizationId;
  final String organizationName;
  final ContactSnapshot contact;

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

  void updateProfile({
    String? organizationId,
    String? organizationName,
    String? firstName,
    String? lastName,
    String? personnelNumber,
    String? phone,
    String? email,
  }) {
    state = state.copyWith(
      organizationId: organizationId,
      organizationName: organizationName,
      contact: ContactSnapshot(
        firstName: firstName ?? state.contact.firstName,
        lastName: lastName ?? state.contact.lastName,
        personnelNumber: personnelNumber ?? state.contact.personnelNumber,
        phone: phone ?? state.contact.phone,
        email: email ?? state.contact.email,
      ),
    );
  }
}

final userProfileProvider =
    NotifierProvider<UserProfileNotifier, UserProfile>(UserProfileNotifier.new);

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';

class SettingsPage extends ConsumerStatefulWidget {
  const SettingsPage({super.key});

  @override
  ConsumerState<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends ConsumerState<SettingsPage> {
  late final TextEditingController _firstNameController;
  late final TextEditingController _lastNameController;
  late final TextEditingController _personnelController;
  late final TextEditingController _phoneController;
  late final TextEditingController _emailController;
  late final TextEditingController _apiUrlController;
  String _selectedOrgId = '11111111-1111-1111-1111-111111111111';

  final _organizations = const [
    ('11111111-1111-1111-1111-111111111111', 'Ukázkový kampus Sever (KAMPUS)'),
    ('22222222-2222-2222-2222-222222222222', 'Ukázkové centrum (CENTRUM)'),
    ('33333333-3333-3333-3333-333333333333', 'Ukázkový sklad (SKLAD)'),
  ];

  @override
  void initState() {
    super.initState();
    final profile = ref.read(userProfileProvider);
    final apiUrl = ref.read(apiBaseUrlProvider);

    _firstNameController = TextEditingController(text: profile.contact.firstName);
    _lastNameController = TextEditingController(text: profile.contact.lastName);
    _personnelController = TextEditingController(text: profile.contact.personnelNumber);
    _phoneController = TextEditingController(text: profile.contact.phone);
    _emailController = TextEditingController(text: profile.contact.email);
    _apiUrlController = TextEditingController(text: apiUrl);
    _selectedOrgId = profile.organizationId;
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _personnelController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _apiUrlController.dispose();
    super.dispose();
  }

  void _save() {
    final org = _organizations.firstWhere(
      (o) => o.$1 == _selectedOrgId,
      orElse: () => _organizations.first,
    );

    ref.read(userProfileProvider.notifier).updateProfile(
          organizationId: org.$1,
          organizationName: org.$2,
          firstName: _firstNameController.text.trim(),
          lastName: _lastNameController.text.trim(),
          personnelNumber: _personnelController.text.trim(),
          phone: _phoneController.text.trim(),
          email: _emailController.text.trim(),
        );

    ref.read(apiBaseUrlProvider.notifier).setUrl(_apiUrlController.text.trim());

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Nastavení profilu bylo uloženo.')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: const Color(0xFF0F172A),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(Icons.manage_accounts_rounded, color: Colors.white, size: 24),
              ),
              const SizedBox(width: 14),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Nastavení profilu a velínu',
                      style: TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                        color: Color(0xFF0F172A),
                      ),
                    ),
                    SizedBox(height: 2),
                    Text(
                      'Parametry oznamovatele a spojení s krizovým centrem',
                      style: TextStyle(fontSize: 13, color: Color(0xFF64748B)),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),

          // Card 1: Profil a organizace
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 4,
                      height: 14,
                      decoration: BoxDecoration(
                        color: const Color(0xFF2563EB),
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'IDENTIFIKACE OZNAMOVATELE',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.7,
                        color: Color(0xFF475569),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                const Text(
                  'Tyto údaje se automaticky přikládají k nouzovému hlášení pro operátory velínu.',
                  style: TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                ),
                const SizedBox(height: 18),

                DropdownButtonFormField<String>(
                  initialValue: _selectedOrgId,
                  decoration: const InputDecoration(
                    labelText: 'Přiřazená organizace / areál',
                    prefixIcon: Icon(Icons.business_rounded, size: 20, color: Color(0xFF64748B)),
                  ),
                  items: [
                    for (final org in _organizations)
                      DropdownMenuItem(
                        value: org.$1,
                        child: Text(
                          org.$2,
                          style: const TextStyle(fontSize: 14, color: Color(0xFF0F172A)),
                        ),
                      ),
                  ],
                  onChanged: (val) {
                    if (val != null) setState(() => _selectedOrgId = val);
                  },
                ),
                const SizedBox(height: 14),

                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _firstNameController,
                        decoration: const InputDecoration(
                          labelText: 'Jméno',
                          prefixIcon: Icon(Icons.person_outline_rounded, size: 20, color: Color(0xFF64748B)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextField(
                        controller: _lastNameController,
                        decoration: const InputDecoration(
                          labelText: 'Příjmení',
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                TextField(
                  controller: _personnelController,
                  decoration: const InputDecoration(
                    labelText: 'Osobní číslo (zaměstnanec / student)',
                    prefixIcon: Icon(Icons.badge_outlined, size: 20, color: Color(0xFF64748B)),
                  ),
                ),
                const SizedBox(height: 14),

                TextField(
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                    labelText: 'Telefonní číslo',
                    prefixIcon: Icon(Icons.phone_outlined, size: 20, color: Color(0xFF64748B)),
                  ),
                ),
                const SizedBox(height: 14),

                TextField(
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(
                    labelText: 'E-mailová adresa',
                    prefixIcon: Icon(Icons.mail_outline_rounded, size: 20, color: Color(0xFF64748B)),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Card 2: Technické připojení
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 4,
                      height: 14,
                      decoration: BoxDecoration(
                        color: const Color(0xFF059669),
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'TECHNICKÉ PŘIPOJENÍ',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.7,
                        color: Color(0xFF475569),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                const Text(
                  'Adresa REST API pro dispečink a synchronizaci bezpečnostních událostí.',
                  style: TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                ),
                const SizedBox(height: 16),

                TextField(
                  controller: _apiUrlController,
                  decoration: const InputDecoration(
                    labelText: 'Adresa .NET API (Krizovka.Api)',
                    hintText: 'http://127.0.0.1:5080',
                    prefixIcon: Icon(Icons.dns_outlined, size: 20, color: Color(0xFF64748B)),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          FilledButton.icon(
            onPressed: _save,
            icon: const Icon(Icons.check_circle_outline_rounded, size: 20),
            label: const Text(
              'Uložit nastavení',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
            ),
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFF0F172A),
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/onboarding/state/onboarding_state.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

class OnboardingDialog extends ConsumerStatefulWidget {
  const OnboardingDialog({super.key});

  static Future<void> show(BuildContext context) {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (context) => const OnboardingDialog(),
    );
  }

  @override
  ConsumerState<OnboardingDialog> createState() => _OnboardingDialogState();
}

class _OnboardingDialogState extends ConsumerState<OnboardingDialog> {
  late final TextEditingController _firstNameController;
  late final TextEditingController _lastNameController;
  late final TextEditingController _phoneController;
  late final TextEditingController _personnelController;
  late final TextEditingController _emailController;

  @override
  void initState() {
    super.initState();
    final profile = ref.read(userProfileProvider);
    _firstNameController =
        TextEditingController(text: profile.contact.firstName ?? '');
    _lastNameController =
        TextEditingController(text: profile.contact.lastName ?? '');
    _phoneController = TextEditingController(text: profile.contact.phone ?? '');
    _personnelController =
        TextEditingController(text: profile.contact.personnelNumber ?? '');
    _emailController = TextEditingController(text: profile.contact.email ?? '');
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _personnelController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _handleSave() async {
    final notifier = ref.read(onboardingNotifierProvider.notifier);
    await notifier.completeOnboarding(
      firstName: _firstNameController.text.trim(),
      lastName: _lastNameController.text.trim(),
      phone: _phoneController.text.trim(),
      personnelNumber: _personnelController.text.trim(),
      email: _emailController.text.trim(),
    );

    if (mounted) {
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vítejte! Váš profil byl bezpečně uložen v zařízení.'),
          duration: Duration(seconds: 3),
        ),
      );
    }
  }

  Future<void> _handleSkip() async {
    final notifier = ref.read(onboardingNotifierProvider.notifier);
    await notifier.skipOnboarding();

    if (mounted) {
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Profil můžete kdykoliv vyplnit v sekci Nastavení.'),
          duration: Duration(seconds: 3),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      backgroundColor: Colors.white,
      insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 680),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFEF2F2),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(
                      Icons.shield_rounded,
                      color: Color(0xFFDC2626),
                      size: 26,
                    ),
                  ),
                  const SizedBox(width: 14),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Vítejte v Krizovce',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                            color: Color(0xFF0F172A),
                          ),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'Krizový a tísňový kanál vaší organizace',
                          style: TextStyle(
                            fontSize: 12,
                            color: Color(0xFF64748B),
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              const Divider(height: 1, color: Color(0xFFE2E8F0)),
              const SizedBox(height: 14),

              // Educational explanation cards
              Flexible(
                child: SingleChildScrollView(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _buildInfoCard(
                        icon: Icons.sos_rounded,
                        iconColor: const Color(0xFFDC2626),
                        bgColor: const Color(0xFFFEF2F2),
                        title: 'Co je Krizovka a k čemu slouží?',
                        description:
                            'Diskrétní tísňový systém areálu. Umožňuje okamžitě a tiše přivolat pomoc operátorů velínu jedním stiskem tlačítka SOS, doposílat zprávy a fotografie i číst bezpečnostní metodiky offline.',
                      ),
                      const SizedBox(height: 10),
                      _buildInfoCard(
                        icon: Icons.badge_outlined,
                        iconColor: const Color(0xFF2563EB),
                        bgColor: const Color(0xFFEFF6FF),
                        title: 'Proč vyplnit kontaktní profil?',
                        description:
                            'Jméno, telefon a osobní/studentské číslo slouží velínu a záchranným složkám k bleskové identifikaci ohrožené osoby a koordinaci zásahu přímo na místě.',
                      ),
                      const SizedBox(height: 10),
                      _buildInfoCard(
                        icon: Icons.lock_outline_rounded,
                        iconColor: const Color(0xFF059669),
                        bgColor: const Color(0xFFECFDF5),
                        title: 'Vaše soukromí (GDPR minimalizace)',
                        description:
                            'Údaje jsou uloženy výhradně v tomto zařízení. Žádný centrální účet se nezakládá. Data se na velín předají pouze v případě, že odešlete tísňové hlášení.',
                      ),
                      const SizedBox(height: 18),

                      // Profile form
                      const Text(
                        'Kontaktní údaje oznamovatele',
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFF0F172A),
                        ),
                      ),
                      const SizedBox(height: 10),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: _firstNameController,
                              textCapitalization: TextCapitalization.words,
                              decoration: const InputDecoration(
                                labelText: 'Jméno',
                                isDense: true,
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: TextField(
                              controller: _lastNameController,
                              textCapitalization: TextCapitalization.words,
                              decoration: const InputDecoration(
                                labelText: 'Příjmení',
                                isDense: true,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      TextField(
                        controller: _phoneController,
                        keyboardType: TextInputType.phone,
                        decoration: const InputDecoration(
                          labelText: 'Telefonní číslo',
                          hintText: '+420 777 123 456',
                          isDense: true,
                          prefixIcon: Icon(Icons.phone_outlined, size: 18),
                        ),
                      ),
                      const SizedBox(height: 10),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: _personnelController,
                              decoration: const InputDecoration(
                                labelText: 'Osobní / stud. číslo',
                                hintText: 'EMP-1002',
                                isDense: true,
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: TextField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              decoration: const InputDecoration(
                                labelText: 'E-mail (volitelný)',
                                isDense: true,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 16),
              const Divider(height: 1, color: Color(0xFFE2E8F0)),
              const SizedBox(height: 14),

              // Action buttons
              Wrap(
                alignment: WrapAlignment.end,
                crossAxisAlignment: WrapCrossAlignment.center,
                spacing: 10,
                runSpacing: 8,
                children: [
                  TextButton(
                    onPressed: _handleSkip,
                    child: const Text(
                      'Doplnit později',
                      style: TextStyle(color: Color(0xFF64748B)),
                    ),
                  ),
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF0F172A),
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 18,
                        vertical: 12,
                      ),
                    ),
                    onPressed: _handleSave,
                    icon: const Icon(Icons.check_rounded, size: 18),
                    label: const Text(
                      'Uložit a pokračovat',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoCard({
    required IconData icon,
    required Color iconColor,
    required Color bgColor,
    required String title,
    required String description,
  }) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: bgColor,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, color: iconColor, size: 18),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF0F172A),
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  description,
                  style: const TextStyle(
                    fontSize: 11,
                    color: Color(0xFF475569),
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:krizovka_client/features/geofencing/state/geofence_state.dart';
import 'package:krizovka_client/features/guides/guides_page.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';
import 'package:krizovka_client/features/reports/state/emergency_state.dart';
import 'package:krizovka_client/features/settings/settings_page.dart';

GoRouter createRouter() => GoRouter(
  routes: [
    ShellRoute(
      builder: (context, state, child) => _AppShell(child: child),
      routes: [
        GoRoute(path: '/', builder: (context, state) => const _ReportsPage()),
        GoRoute(
          path: '/metodiky',
          builder: (context, state) => const GuidesPage(),
        ),
        GoRoute(
          path: '/nastaveni',
          builder: (context, state) => const SettingsPage(),
        ),
      ],
    ),
  ],
);

class KrizovkaApp extends StatelessWidget {
  const KrizovkaApp({super.key, required this.router});
  final GoRouter router;

  @override
  Widget build(BuildContext context) => MaterialApp.router(
    title: 'Krizovka',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      useMaterial3: true,
      scaffoldBackgroundColor: const Color(0xFFF8FAFC),
      colorScheme: const ColorScheme.light(
        primary: Color(0xFFDC2626),
        onPrimary: Colors.white,
        surface: Colors.white,
        onSurface: Color(0xFF0F172A),
        surfaceContainerHighest: Color(0xFFF1F5F9),
        outline: Color(0xFFCBD5E1),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: Colors.white,
        foregroundColor: Color(0xFF0F172A),
        elevation: 0,
        scrolledUnderElevation: 0,
        shape: Border(bottom: BorderSide(color: Color(0xFFE2E8F0), width: 1)),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.white,
        elevation: 0,
        indicatorColor: const Color(0xFFF1F5F9),
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const TextStyle(
              color: Color(0xFF0F172A),
              fontWeight: FontWeight.w700,
              fontSize: 12,
            );
          }
          return const TextStyle(
            color: Color(0xFF64748B),
            fontWeight: FontWeight.w500,
            fontSize: 12,
          );
        }),
        iconTheme: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const IconThemeData(color: Color(0xFF0F172A));
          }
          return const IconThemeData(color: Color(0xFF64748B));
        }),
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFCBD5E1)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFCBD5E1)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFF0F172A), width: 1.5),
        ),
      ),
    ),
    routerConfig: router,
  );
}

class _AppShell extends ConsumerStatefulWidget {
  const _AppShell({required this.child});
  final Widget child;

  @override
  ConsumerState<_AppShell> createState() => _AppShellState();
}

class _AppShellState extends ConsumerState<_AppShell> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(geofenceNotifierProvider.notifier).bootstrap();
    });
  }

  @override
  Widget build(BuildContext context) {
    final geofenceState = ref.watch(geofenceNotifierProvider);
    final path = GoRouterState.of(context).uri.path;
    return Scaffold(
      appBar: AppBar(
        title: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.shield_rounded, color: Color(0xFFDC2626), size: 22),
            SizedBox(width: 8),
            Text(
              'KRIZOVKA',
              style: TextStyle(
                fontWeight: FontWeight.w900,
                fontSize: 18,
                letterSpacing: 0.8,
                color: Color(0xFF0F172A),
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Nastavení',
            onPressed: () => context.go('/nastaveni'),
            icon: const Icon(Icons.settings_outlined, color: Color(0xFF334155)),
          ),
        ],
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 680),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Container(
                  decoration: const BoxDecoration(
                    color: Color(0xFFF1F5F9),
                    border: Border(
                      bottom: BorderSide(color: Color(0xFFE2E8F0)),
                    ),
                  ),
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                  child: Row(
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          color: Color(0xFF16A34A),
                          shape: BoxShape.circle,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          geofenceState.displayLabel,
                          style: const TextStyle(
                            color: Color(0xFF1E293B),
                            fontWeight: FontWeight.w600,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(child: widget.child),
              ],
            ),
          ),
        ),
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
        ),
        child: NavigationBar(
          selectedIndex: path == '/metodiky' ? 1 : 0,
          onDestinationSelected: (index) =>
              context.go(index == 0 ? '/' : '/metodiky'),
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.sos_outlined),
              selectedIcon: Icon(Icons.sos, color: Color(0xFFDC2626)),
              label: 'Hlášení',
            ),
            NavigationDestination(
              icon: Icon(Icons.menu_book_outlined),
              selectedIcon: Icon(Icons.menu_book, color: Color(0xFF0F172A)),
              label: 'Metodiky',
            ),
          ],
        ),
      ),
    );
  }
}

class _ReportsPage extends ConsumerStatefulWidget {
  const _ReportsPage();

  @override
  ConsumerState<_ReportsPage> createState() => _ReportsPageState();
}

class _ReportsPageState extends ConsumerState<_ReportsPage> {
  final TextEditingController _msgController = TextEditingController();

  @override
  void dispose() {
    _msgController.dispose();
    super.dispose();
  }

  Future<void> _showTextReportDialog(bool isEmergencyActive) async {
    final textController = TextEditingController();
    final notifier = ref.read(emergencyReportNotifierProvider.notifier);

    final submitted = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Row(
          children: [
            Icon(
              isEmergencyActive ? Icons.chat_bubble_outline : Icons.edit_note_rounded,
              color: const Color(0xFFDC2626),
            ),
            const SizedBox(width: 8),
            Text(isEmergencyActive ? 'Doplňující zpráva' : 'Textové hlášení'),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              isEmergencyActive
                  ? 'Doplňte podrobnosti k aktivnímu incidentu pro operátora velínu:'
                  : 'Stručně popište krizovou situaci (kde se nacházíte, co se děje):',
              style: const TextStyle(fontSize: 13, color: Color(0xFF64748B)),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: textController,
              maxLines: 4,
              autofocus: true,
              decoration: const InputDecoration(
                hintText: 'Např. Požár v 2. patře, silný dým na chodbě...',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Zrušit'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFDC2626),
            ),
            onPressed: () {
              if (textController.text.trim().isNotEmpty) {
                Navigator.of(ctx).pop(true);
              }
            },
            child: Text(isEmergencyActive ? 'Odeslat velínu' : 'Odeslat hlášení'),
          ),
        ],
      ),
    );

    if (submitted == true && textController.text.trim().isNotEmpty) {
      if (isEmergencyActive) {
        await notifier.sendSupplementalMessage(textController.text.trim());
      } else {
        await notifier.sendTextReport(textController.text.trim());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final emergencyState = ref.watch(emergencyReportNotifierProvider);
    final notifier = ref.read(emergencyReportNotifierProvider.notifier);
    final baseUrl = ref.watch(apiBaseUrlProvider);
    final isEmergencyActive = emergencyState.isEmergencyActive;

    final supplementalMessages = emergencyState.report?.history
            .where((h) => h.action == 'MessageAdded')
            .toList() ??
        const [];

    return ListView(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
      children: [
        Text(
          'Rychlé hlášení',
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                fontWeight: FontWeight.w800,
                color: const Color(0xFF0F172A),
              ),
        ),
        const SizedBox(height: 4),
        const Text(
          'Pomoc, kontakty a dostupná poloha na jednom místě.',
          style: TextStyle(color: Color(0xFF64748B), fontSize: 13),
        ),
        const SizedBox(height: 20),

        // Emergency action / state display
        switch (emergencyState.phase) {
          EmergencyPhase.idle => FilledButton(
              style: FilledButton.styleFrom(
                backgroundColor: const Color(0xFFB91C1C),
                foregroundColor: Colors.white,
                elevation: 4,
                shadowColor: const Color(0x66B91C1C),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(18),
                  side: const BorderSide(color: Color(0xFFEF4444), width: 1.5),
                ),
                padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
              ),
              onPressed: () => notifier.sendEmergencyReport(),
              child: const SizedBox(
                width: double.infinity,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.crisis_alert_rounded,
                          color: Colors.white,
                          size: 26,
                        ),
                        SizedBox(width: 10),
                        Flexible(
                          child: Text(
                            'POTŘEBUJI POMOC',
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.w900,
                              letterSpacing: 0.8,
                            ),
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Text(
                      'TICHÝ REŽIM • OKAMŽITÉ ODESLÁNÍ NA VELÍN • REAKCE DO 3 S',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 10,
                        color: Colors.white70,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.6,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          EmergencyPhase.sending => Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: const Color(0xFFFEF2F2),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: const Color(0xFFFECACA), width: 1.5),
              ),
              child: const Row(
                children: [
                  SizedBox(
                    width: 24,
                    height: 24,
                    child: CircularProgressIndicator(
                      strokeWidth: 3,
                      color: Color(0xFFDC2626),
                    ),
                  ),
                  SizedBox(width: 16),
                  Expanded(
                    child: Text(
                      'Odesílám nouzové hlášení na velín...',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 15,
                        color: Color(0xFF991B1B),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          EmergencyPhase.submitted => Card(
              color: const Color(0xFFFFFBEB),
              shape: RoundedRectangleBorder(
                side: const BorderSide(color: Color(0xFFFDE68A), width: 1.5),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        Container(
                          width: 32,
                          height: 32,
                          padding: const EdgeInsets.all(6),
                          decoration: const BoxDecoration(
                            color: Color(0xFFFEF3C7),
                            shape: BoxShape.circle,
                          ),
                          child: const CircularProgressIndicator(
                            strokeWidth: 3,
                            color: Color(0xFFB45309),
                          ),
                        ),
                        const SizedBox(width: 14),
                        const Expanded(
                          child: Text(
                            'Hlášení doručeno – Čeká na velín',
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 16,
                              color: Color(0xFF92400E),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFFFDE68A)),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.tag, size: 16, color: Color(0xFFB45309)),
                          const SizedBox(width: 6),
                          Expanded(
                            child: Text(
                              'ID: ${emergencyState.report?.id ?? emergencyState.clientReportId}',
                              style: const TextStyle(
                                fontSize: 12,
                                fontFamily: 'monospace',
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF78350F),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (emergencyState.report?.description != null &&
                        emergencyState.report!.description!.isNotEmpty) ...[
                      const SizedBox(height: 10),
                      Text(
                        'Popis: ${emergencyState.report!.description}',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: Color(0xFF78350F),
                        ),
                      ),
                    ],
                    const SizedBox(height: 10),
                    const Row(
                      children: [
                        Icon(Icons.volume_off, size: 15, color: Color(0xFFB45309)),
                        SizedBox(width: 6),
                        Text(
                          'Tichý režim aktivní: žádný zvuk ani blesk.',
                          style: TextStyle(fontSize: 12, color: Color(0xFFB45309)),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          EmergencyPhase.accepted => Card(
              color: const Color(0xFFECFDF5),
              shape: RoundedRectangleBorder(
                side: const BorderSide(color: Color(0xFFA7F3D0), width: 1.5),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Row(
                      children: [
                        Icon(Icons.check_circle_rounded, color: Color(0xFF059669), size: 30),
                        SizedBox(width: 14),
                        Expanded(
                          child: Text(
                            'Hlášení převzato operátorem!',
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 18,
                              color: Color(0xFF065F46),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      'Operátor velínu převzal vaše hlášení. Pomoc je na cestě a koordinována velínem.',
                      style: TextStyle(color: Color(0xFF065F46), fontSize: 14, height: 1.4),
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFFA7F3D0)),
                      ),
                      child: Text(
                        'Stav: Převzato (Accepted) · ID: ${emergencyState.report?.id}',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: Color(0xFF047857),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          EmergencyPhase.closed => Card(
              color: const Color(0xFFF8FAFC),
              shape: RoundedRectangleBorder(
                side: const BorderSide(color: Color(0xFFCBD5E1)),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.all(22),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Row(
                      children: [
                        Icon(Icons.done_all_rounded, color: Color(0xFF475569), size: 28),
                        SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Hlášení bylo velínem uzavřeno',
                            style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16, color: Color(0xFF1E293B)),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Krizová událost byla úspěšně vyřešena.',
                      style: TextStyle(color: Color(0xFF64748B)),
                    ),
                    const SizedBox(height: 16),
                    OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: const Color(0xFF0F172A),
                        side: const BorderSide(color: Color(0xFFCBD5E1)),
                      ),
                      onPressed: () => notifier.reset(),
                      child: const Text('Zpět na začátek'),
                    ),
                  ],
                ),
              ),
            ),
          EmergencyPhase.error => Card(
              color: const Color(0xFFFEF2F2),
              shape: RoundedRectangleBorder(
                side: const BorderSide(color: Color(0xFFFECACA), width: 1.5),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Row(
                      children: [
                        Icon(Icons.error_outline_rounded, color: Color(0xFFDC2626)),
                        SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Chyba při odesílání hlášení',
                            style: TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                              color: Color(0xFF991B1B),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      emergencyState.errorMessage ?? 'Nepodařilo se navázat spojení se serverem.',
                      style: const TextStyle(fontSize: 13, color: Color(0xFF991B1B)),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        TextButton(
                          onPressed: () => notifier.reset(),
                          child: const Text('Zrušit'),
                        ),
                        const SizedBox(width: 8),
                        FilledButton.icon(
                          style: FilledButton.styleFrom(
                            backgroundColor: const Color(0xFFDC2626),
                          ),
                          onPressed: () => notifier.retry(),
                          icon: const Icon(Icons.refresh, size: 18),
                          label: const Text('Zkusit znovu'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
        },

        // Live stream active banner
        if (emergencyState.isStreaming) ...[
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: const Color(0xFFFEF2F2),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFFFCA5A5), width: 1.5),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Container(
                      width: 12,
                      height: 12,
                      decoration: const BoxDecoration(
                        color: Color(0xFFDC2626),
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'ŽIVÝ PŘENOS AKTIVNÍ (${emergencyState.streamMode?.toUpperCase() ?? 'UPLINK'})',
                        style: const TextStyle(
                          fontWeight: FontWeight.w900,
                          fontSize: 13,
                          color: Color(0xFF991B1B),
                          letterSpacing: 0.5,
                        ),
                      ),
                    ),
                    OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: const Color(0xFFDC2626),
                        side: const BorderSide(color: Color(0xFFEF4444)),
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      ),
                      onPressed: () => notifier.endLiveStream(),
                      child: const Text('Ukončit přenos', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 12)),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                const Text(
                  'Striktní jednosměrný přenos: Telefon vysílá mikrofon/kameru, z reproduktoru nevychází ŽÁDNÝ zvuk.',
                  style: TextStyle(fontSize: 11, color: Color(0xFF7F1D1D), fontWeight: FontWeight.w500),
                ),
              ],
            ),
          ),
        ],

        // Audio recording banner
        if (emergencyState.isRecordingAudio) ...[
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: const Color(0xFFFFFBEB),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFFFCD34D), width: 1.5),
            ),
            child: Row(
              children: [
                const Icon(Icons.mic, color: Color(0xFFB45309), size: 24),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    'Nahrávám: 0:${emergencyState.recordingDurationSeconds.toString().padLeft(2, '0')} / 1:00',
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF92400E),
                      fontSize: 13,
                    ),
                  ),
                ),
                TextButton(
                  onPressed: () => notifier.cancelAudioRecording(),
                  child: const Text('Zrušit', style: TextStyle(color: Color(0xFF92400E))),
                ),
                const SizedBox(width: 4),
                FilledButton(
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFFB45309),
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  ),
                  onPressed: () => notifier.stopAudioRecordingAndUpload(),
                  child: const Text('Odeslat', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                ),
              ],
            ),
          ),
        ],

        // Attachment uploading indicator
        if (emergencyState.isUploadingAttachment) ...[
          const SizedBox(height: 12),
          const Row(
            children: [
              SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
              SizedBox(width: 8),
              Text('Nahrávám přílohu...', style: TextStyle(fontSize: 12, color: Color(0xFF64748B))),
            ],
          ),
        ],

        // Attachment error
        if (emergencyState.attachmentError != null) ...[
          const SizedBox(height: 8),
          Text(
            emergencyState.attachmentError!,
            style: const TextStyle(fontSize: 12, color: Color(0xFFDC2626)),
          ),
        ],

        // Active Emergency: Attachments Preview & Supplemental Messages
        if (isEmergencyActive && emergencyState.report != null) ...[
          const SizedBox(height: 20),

          // Attachments preview
          if (emergencyState.report!.attachments.isNotEmpty) ...[
            Text(
              'Přiložená média (${emergencyState.report!.attachments.length})',
              style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 14,
                color: Color(0xFF0F172A),
              ),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 10,
              runSpacing: 10,
              children: [
                for (final att in emergencyState.report!.attachments)
                  _AttachmentTile(
                    attachment: att,
                    reportId: emergencyState.report!.id,
                    baseUrl: baseUrl,
                  ),
              ],
            ),
            const SizedBox(height: 16),
          ],

          // Supplemental messages thread
          Text(
            'Doplňující zprávy k incidentu',
            style: const TextStyle(
              fontWeight: FontWeight.w800,
              fontSize: 14,
              color: Color(0xFF0F172A),
            ),
          ),
          const SizedBox(height: 8),
          if (supplementalMessages.isEmpty)
            const Text(
              'Zatím nebyly zaslány doplňující zprávy.',
              style: TextStyle(fontSize: 12, color: Color(0xFF64748B), fontStyle: FontStyle.italic),
            )
          else
            for (final msg in supplementalMessages)
              Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      msg.newValue ?? '',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF1E293B),
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Odesláno: ${msg.createdAt.hour.toString().padLeft(2, '0')}:${msg.createdAt.minute.toString().padLeft(2, '0')}:${msg.createdAt.second.toString().padLeft(2, '0')}',
                      style: const TextStyle(fontSize: 11, color: Color(0xFF94A3B8)),
                    ),
                  ],
                ),
              ),

          const SizedBox(height: 10),
          // Input row for supplemental message
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _msgController,
                  decoration: const InputDecoration(
                    hintText: 'Doposlat zprávu velínu...',
                    isDense: true,
                    contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                  ),
                  onSubmitted: (val) {
                    if (val.trim().isNotEmpty) {
                      notifier.sendSupplementalMessage(val.trim());
                      _msgController.clear();
                    }
                  },
                ),
              ),
              const SizedBox(width: 8),
              IconButton.filled(
                style: IconButton.styleFrom(backgroundColor: const Color(0xFF0F172A)),
                onPressed: emergencyState.isSendingMessage
                    ? null
                    : () {
                        if (_msgController.text.trim().isNotEmpty) {
                          notifier.sendSupplementalMessage(_msgController.text.trim());
                          _msgController.clear();
                        }
                      },
                icon: emergencyState.isSendingMessage
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.send_rounded, size: 18),
              ),
            ],
          ),
          if (emergencyState.messageError != null) ...[
            const SizedBox(height: 4),
            Text(emergencyState.messageError!, style: const TextStyle(fontSize: 12, color: Color(0xFFDC2626))),
          ],
        ],

        const SizedBox(height: 32),
        Text(
          isEmergencyActive ? 'Doplnit k probíhajícímu hlášení' : 'Možnosti hlášení',
          style: const TextStyle(
            fontWeight: FontWeight.w800,
            fontSize: 15,
            color: Color(0xFF0F172A),
          ),
        ),
        const SizedBox(height: 12),

        // Reporting Options
        _ReportingOptionTile(
          icon: Icons.edit_outlined,
          title: 'Text',
          subtitle: isEmergencyActive
              ? 'Doposlat doplňující zprávu k incidentu.'
              : 'Krátká zpráva s popisem situace.',
          actionLabel: isEmergencyActive ? 'Napsat' : 'Zadat',
          onTap: () => _showTextReportDialog(isEmergencyActive),
        ),
        _ReportingOptionTile(
          icon: Icons.photo_camera_outlined,
          title: 'Foto z místa',
          subtitle: 'Pořídit nebo vybrat fotografii ze situace.',
          actionLabel: 'Vyfotit',
          onTap: () async {
            if (!isEmergencyActive) {
              await notifier.sendEmergencyReport(description: 'Hlášení s fotografií – POTŘEBUJI POMOC');
            }
            await notifier.pickAndUploadPhoto();
          },
        ),
        _ReportingOptionTile(
          icon: Icons.mic_none_outlined,
          title: 'Hlasovka',
          subtitle: emergencyState.isRecordingAudio
              ? 'Nahrávání probíhá...'
              : 'Nahrát a odeslat hlasovou zprávu.',
          actionLabel: emergencyState.isRecordingAudio ? 'Nahrává' : 'Nahrát',
          isActive: emergencyState.isRecordingAudio,
          onTap: () async {
            if (emergencyState.isRecordingAudio) {
              await notifier.stopAudioRecordingAndUpload();
            } else {
              if (!isEmergencyActive) {
                await notifier.sendEmergencyReport(description: 'Hlasová zpráva – POTŘEBUJI POMOC');
              }
              await notifier.startAudioRecording();
            }
          },
        ),
        _ReportingOptionTile(
          icon: Icons.sensors_outlined,
          title: 'Živě audio',
          subtitle: emergencyState.isStreaming && emergencyState.streamMode == 'audio'
              ? 'Přímý přenos zvuku probíhá.'
              : 'Přímý přenos mikrofonu do velínu.',
          actionLabel: emergencyState.isStreaming && emergencyState.streamMode == 'audio' ? 'Vysílá' : 'Spustit',
          isActive: emergencyState.isStreaming && emergencyState.streamMode == 'audio',
          onTap: () async {
            if (emergencyState.isStreaming) {
              await notifier.endLiveStream();
            } else {
              await notifier.startLiveStream('audio');
            }
          },
        ),
        _ReportingOptionTile(
          icon: Icons.videocam_outlined,
          title: 'Živě video',
          subtitle: emergencyState.isStreaming && emergencyState.streamMode == 'video'
              ? 'Přímý přenos videa a audia probíhá.'
              : 'Přímý přenos kamery a mikrofonu.',
          actionLabel: emergencyState.isStreaming && emergencyState.streamMode == 'video' ? 'Vysílá' : 'Spustit',
          isActive: emergencyState.isStreaming && emergencyState.streamMode == 'video',
          onTap: () async {
            if (emergencyState.isStreaming) {
              await notifier.endLiveStream();
            } else {
              await notifier.startLiveStream('video');
            }
          },
        ),
      ],
    );
  }
}

class _ReportingOptionTile extends StatelessWidget {
  const _ReportingOptionTile({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.actionLabel,
    required this.onTap,
    this.isActive = false,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String actionLabel;
  final VoidCallback onTap;
  final bool isActive;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: isActive ? const Color(0xFFFEF2F2) : Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isActive ? const Color(0xFFEF4444) : const Color(0xFFE2E8F0),
          width: isActive ? 1.5 : 1,
        ),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(12),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: isActive ? const Color(0xFFFEE2E2) : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(
                    icon,
                    size: 20,
                    color: isActive ? const Color(0xFFDC2626) : const Color(0xFF0F172A),
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: TextStyle(
                          fontWeight: FontWeight.w700,
                          fontSize: 14,
                          color: isActive ? const Color(0xFF991B1B) : const Color(0xFF0F172A),
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        style: TextStyle(
                          fontSize: 12,
                          color: isActive ? const Color(0xFFB91C1C) : const Color(0xFF64748B),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: isActive ? const Color(0xFFDC2626) : const Color(0xFF0F172A),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    actionLabel,
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: Colors.white,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _AttachmentTile extends StatelessWidget {
  const _AttachmentTile({
    required this.attachment,
    required this.reportId,
    required this.baseUrl,
  });

  final ReportAttachment attachment;
  final String reportId;
  final String baseUrl;

  @override
  Widget build(BuildContext context) {
    final isImage = attachment.contentType.startsWith('image/');
    final isAudio = attachment.contentType.startsWith('audio/') ||
        attachment.fileName.endsWith('.m4a') ||
        attachment.fileName.endsWith('.mp3');

    final url = '$baseUrl/api/reports/$reportId/attachments/${attachment.id}';

    return Container(
      width: 160,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFCBD5E1)),
      ),
      padding: const EdgeInsets.all(8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (isImage)
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: SizedBox(
                height: 90,
                width: double.infinity,
                child: Image.network(
                  url,
                  fit: BoxFit.cover,
                  errorBuilder: (_, _, _) => Container(
                    color: const Color(0xFFF1F5F9),
                    child: const Icon(Icons.image, color: Color(0xFF94A3B8)),
                  ),
                ),
              ),
            )
          else if (isAudio)
            Container(
              height: 70,
              width: double.infinity,
              decoration: BoxDecoration(
                color: const Color(0xFFFEF3C7),
                borderRadius: BorderRadius.circular(6),
              ),
              child: const Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.mic, color: Color(0xFFB45309), size: 24),
                  SizedBox(height: 2),
                  Text(
                    'Hlasová zpráva',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: Color(0xFF92400E)),
                  ),
                ],
              ),
            )
          else
            Container(
              height: 70,
              width: double.infinity,
              decoration: BoxDecoration(
                color: const Color(0xFFF1F5F9),
                borderRadius: BorderRadius.circular(6),
              ),
              child: const Icon(Icons.attach_file, color: Color(0xFF64748B), size: 24),
            ),
          const SizedBox(height: 6),
          Text(
            attachment.fileName,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600),
          ),
          Text(
            '${(attachment.sizeBytes / 1024).toStringAsFixed(1)} KB',
            style: const TextStyle(fontSize: 10, color: Color(0xFF64748B)),
          ),
        ],
      ),
    );
  }
}

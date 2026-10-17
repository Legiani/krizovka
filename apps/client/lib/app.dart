import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

GoRouter createRouter() => GoRouter(
  routes: [
    ShellRoute(
      builder: (context, state, child) => _AppShell(child: child),
      routes: [
        GoRoute(path: '/', builder: (context, state) => const _ReportsPage()),
        GoRoute(
          path: '/metodiky',
          builder: (context, state) => const _Placeholder(
            title: 'Metodiky',
            text: 'Zde bude manuál vaší organizace, jak se zachovat v krizových situacích.',
          ),
        ),
        GoRoute(
          path: '/nastaveni',
          builder: (context, state) => const _Placeholder(
            title: 'Nastavení',
            text:
                'Zde připravíte své kontakty, organizaci a oprávnění telefonu. '
                'Uložení údajů zatím není zapojeno.',
          ),
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
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFFB91C1C)),
      useMaterial3: true,
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
    ),
    routerConfig: router,
  );
}

class _AppShell extends StatelessWidget {
  const _AppShell({required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final path = GoRouterState.of(context).uri.path;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Krizovka'),
        actions: [
          IconButton(
            tooltip: 'Nastavení',
            onPressed: () => context.go('/nastaveni'),
            icon: const Icon(Icons.settings_outlined),
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
                  color: const Color(0xFFFFF3CD),
                  padding: const EdgeInsets.all(16),
                  child: const Text(
                    'Vývojová verze. Hlášení se zatím neodesílají. '
                    'Přihlášení a organizace nejsou připojeny.',
                    style: TextStyle(color: Color(0xFF664D03)),
                  ),
                ),
                Expanded(child: child),
              ],
            ),
          ),
        ),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: path == '/metodiky' ? 1 : 0,
        onDestinationSelected: (index) =>
            context.go(index == 0 ? '/' : '/metodiky'),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.sos_outlined),
            label: 'Hlášení',
          ),
          NavigationDestination(
            icon: Icon(Icons.menu_book_outlined),
            label: 'Metodiky',
          ),
        ],
      ),
    );
  }
}

class _ReportsPage extends StatelessWidget {
  const _ReportsPage();

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(24),
    children: [
      Text('Rychlé hlášení', style: Theme.of(context).textTheme.headlineMedium),
      const SizedBox(height: 12),
      const Text('Pomoc, kontakty a dostupná poloha na jednom místě.'),
      const SizedBox(height: 24),
      const FilledButton(
        onPressed: null,
        child: Padding(
          padding: EdgeInsets.symmetric(vertical: 24),
          child: Text('POTŘEBUJI POMOC', textAlign: TextAlign.center),
        ),
      ),
      const SizedBox(height: 24),
      const Text('Připravované možnosti hlášení'),
      for (final option in const [
        (Icons.edit_outlined, 'Text', 'Krátká zpráva o tom, co se děje.'),
        (
          Icons.mic_none_outlined,
          'Hlasovka',
          'Nahrát a odeslat hlasovou zprávu.',
        ),
        (
          Icons.sensors_outlined,
          'Živě audio',
          'Přímý přenos mikrofonu do velínu.',
        ),
        (
          Icons.videocam_outlined,
          'Živě video',
          'Přímý přenos kamery a mikrofonu.',
        ),
      ])
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: Icon(option.$1),
          title: Text(option.$2),
          subtitle: Text(option.$3),
        ),
    ],
  );
}

class _Placeholder extends StatelessWidget {
  const _Placeholder({required this.title, required this.text});
  final String title;
  final String text;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(24),
    children: [
      Text(title, style: Theme.of(context).textTheme.headlineMedium),
      const SizedBox(height: 16),
      Text(text),
    ],
  );
}

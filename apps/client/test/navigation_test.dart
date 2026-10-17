import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:krizovka_client/app.dart';

void main() {
  testWidgets('navigation and honest development state', (tester) async {
    final router = createRouter();
    addTearDown(router.dispose);
    await tester.pumpWidget(KrizovkaApp(router: router));
    await tester.pumpAndSettle();
    expect(
      tester.widget<FilledButton>(find.byType(FilledButton)).onPressed,
      isNull,
    );
    expect(find.textContaining('Hlášení se zatím neodesílají'), findsOneWidget);
    await tester.tap(find.text('Metodiky'));
    await tester.pumpAndSettle();
    expect(find.textContaining('manuál vaší organizace'), findsOneWidget);
    await tester.tap(find.byTooltip('Nastavení'));
    await tester.pumpAndSettle();
    expect(
      find.textContaining('Uložení údajů zatím není zapojeno'),
      findsOneWidget,
    );
    await tester.tap(find.text('Hlášení'));
    await tester.pumpAndSettle();
    expect(find.text('POTŘEBUJI POMOC'), findsOneWidget);
  });

  testWidgets('narrow screen and large text remain usable', (tester) async {
    tester.view.physicalSize = const Size(375, 667);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
    final router = createRouter();
    addTearDown(router.dispose);
    await tester.pumpWidget(KrizovkaApp(router: router));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.drag(find.byType(ListView), const Offset(0, -500));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });
}

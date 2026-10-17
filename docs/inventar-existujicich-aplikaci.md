# Inventář existujících aplikací pro Krizovku

**Datum průzkumu:** 15. 10. 2026  
**Prohledaný adresář:** `/Users/jakubbednar/Developer`  
**Účel:** určit konkrétní zdrojové základy pro technickou dokumentaci Krizovky.

**Aktuální rozsah:** tento inventář zachycuje širší průzkum, nikoli povinný rozsah vývoje. Platí [technický záměr v0.4](../technicka-dokumentace.md): rychlé hlášení textem, hlasovkou nebo živým audiem/videem, více oddělených organizací, vlastní manuály s převzetím ze společné knihovny a Nastavení s osobními údaji v telefonu či chráněné webové cookie. Mapa je pouze v administraci. Pro první verzi využijeme hlavně Změňte.to, vybrané části IS/DS a Next.js administraci Města+; streamování doplní existující mediální řešení. Offline fronty a SignalR pro seznam hlášení se nyní nepřebírají jako požadavek.

Uživatel označil složku jako „development“. Na tomto počítači odpovídá umístění projektů adresáři `Developer`; samostatný adresář `Development` ani `development` nebyl nalezen. Průzkum byl pouze pro čtení. Zdrojové aplikace nebyly upraveny, kopírovány, spouštěny ani nasazovány.

Byly vyhledány manifesty Flutteru, Next.js a .NET a následně přečteny vybrané implementace relevantní pro hlášení, soubory, autentizaci a dohled. Přítomnost závislosti nebo souboru není důkazem funkčnosti celé aplikace. Buildy, testy a chování na zařízeních nebyly v této dokumentační fázi ověřovány.

## 1. Doporučené zdroje

| Zdroj | Zjištěný stav | Využití v Krizovce |
| --- | --- | --- |
| `OICT_Aplikace/zmente-to-flutter` | Flutter; adresáře Android, iOS a web; Riverpod, GoRouter, Dio, OAuth, fotografie, poloha, hlášení a jejich historie. | Hlavní základ uživatelské aplikace. |
| `city-plus/src/Services/IS` | ASP.NET Core `net10.0`, EF Core a PostgreSQL; podněty, přílohy, operátoři, historie, interní poznámky a slučování. | Výchozí implementace pro modul krizových hlášení. |
| `city-plus/src/Services/DS` | .NET 10; evidence dokumentů, SHA-256, rozhraní úložiště a souborový backend. | Základ modulu médií a příloh. |
| `city-plus/src/Apps/Frontend` | Next.js, React, TypeScript, Bootstrap, TanStack Query a generování klientů Orvalem; modul správy podnětů. | Základ dohledové a administrační aplikace. |
| `city-plus/src/Apps/Mobile` | Flutter; offline fronta podnětů, lokální databáze a ukládání fotografií pro mobilní platformy. | Doplňující zdroj pro mobilní frontu a lokální úložiště. |
| `city-plus/src/Shared/Common` | Sdílené knihovny autentizace, zpracování chyb, EF Core, logování a komunikace. | Výběrové převzetí použitelných částí a konvencí po odstranění vazeb na městské služby. |

### Verze a reprodukovatelnost průzkumu

- Změňte.to: Git HEAD `6f3cb0f8c64b4516be6033d41efb48e8762ccea6`; bez změn sledovaných souborů v době kontroly. `pubspec.yaml` uvádí aplikaci `4.0.0+77`, Dart `^3.9.2`; `.fvmrc` připíná Flutter `3.47.7`.
- Město+: Git HEAD `ba0d7714b14c21b6adf0f6d15e1118f432ae4acd`; při průzkumu obsahoval pracovní strom tři změněné sledované soubory. Závěry vycházejí z přečteného pracovního stromu, nikoli z garantovaně čistého commitu. Před převzetím se musí zafixovat přesný commit a případný rozdíl pracovního stromu.
- Next.js v kořenovém frontendovém manifestu Města+: `16.2.4`, React `19.2.3`. Jde o zjištěné deklarované verze, nikoli potvrzení aktuálnosti nebo bezpečnosti všech závislostí.
- Služba IS cílí na `net10.0`, ale její manifest ještě obsahuje preview verzi `Microsoft.AspNetCore.OpenApi`. Při převzetí se závislosti sjednotí na kompatibilní stabilní verze.

## 2. Flutter: co skutečně převzít

| Zdrojový soubor nebo adresář | Zjištění a potřebná úprava |
| --- | --- |
| [pubspec.yaml](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/pubspec.yaml) | Existuje požadovaný základ Flutter + Riverpod + GoRouter + Dio. Převzít výběr potřebných závislostí a jejich zamčené verze; nepřenášet všechny integrace automaticky. |
| [app_router.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/app/router/app_router.dart) | Základ navigace; upravit pro metodiky, rychlé hlášení, vlastní hlášení a profil. |
| [real_incidents_repository.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/features/incidents/data/real_incidents_repository.dart) | Reálná komunikace se stávajícím API a mapování dat. Nahradit endpointy a DTO kontrakty Krizovky. |
| [report_wizard_notifier.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/features/report/application/report_wizard_notifier.dart) | Vícekrokové hlášení s fotografiemi, polohou a formulářem. Pro krizovou akci změnit pořadí: nejprve vznik hlášení, potom doplnění údajů a souborů. |
| [report_draft_notifier.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/features/report/application/report_draft_notifier.dart) | Tato implementace odmítne odeslání bez polohy a popisu a před založením podnětu nahrává obrázek. Tyto podmínky neodpovídají rychlému krizovému hlášení. |
| [auth_repository.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/features/auth/auth_repository.dart) | OAuth Authorization Code a rozlišení mobilního a webového ukládání tokenů. Konkrétní přihlášení je navázané na Lítačku; pro Krizovku se změní poskytovatel a sjednotí obnova relace. Webové tokeny se zde drží v paměti. |
| [dio_provider.dart](/Users/jakubbednar/Developer/OICT_Aplikace/zmente-to-flutter/lib/core/api/dio_provider.dart) | Výchozí síťová vrstva a práce s přihlášením; přizpůsobit novému API a zvláštním pravidlům pro opakování krizových zápisů. |
| [offline_sync_service_impl.dart](/Users/jakubbednar/Developer/city-plus/src/Apps/Mobile/lib/features/issues/application/offline_sync_service_impl.dart) | Mobilní fronta, uložení fotografií a šifrování serializovaného payloadu. Doplnit stabilní identifikátory, vazbu na účet a organizaci, média a spolehlivé zotavení. |
| [queue_processor_service.dart](/Users/jakubbednar/Developer/city-plus/src/Apps/Mobile/lib/features/issues/application/queue_processor_service.dart) | Existuje zpracování fronty po změně spojení. Stávající pořadí nejprve nahrává fotografie a chybějící soubor může vést ke smazání položky. Pro Krizovku se základní hlášení odešle před médii a chyba přílohy nesmaže celé hlášení. |
| [offline_sync_service_web.dart](/Users/jakubbednar/Developer/city-plus/src/Apps/Mobile/lib/features/issues/application/offline_sync_service_web.dart) | Webová varianta je prázdná implementace: frontu ani koncepty neukládá. Webový offline režim se musí doplnit. |
| [disk_cache_store_web.dart](/Users/jakubbednar/Developer/city-plus/src/Apps/Mobile/lib/core/storage/disk_cache_store_web.dart) | Navzdory názvu ukládá data pouze do paměťové mapy. Neposkytuje trvalou webovou cache. |

Ve zkoumaném reportovacím základu Změňte.to nebyla nalezena hotová krizová implementace audia, videa, garantovaného tichého režimu ani obnovitelného přenosu velkých souborů. Fotografie přes `image_picker` nejsou důkazem splnění těchto požadavků.

## 3. .NET: co skutečně převzít

| Zdrojový soubor nebo adresář | Zjištění a potřebná úprava |
| --- | --- |
| [IS.csproj](/Users/jakubbednar/Developer/city-plus/src/Services/IS/src/Mesto.Plus.IS.Api/Mesto.Plus.IS.Api.csproj) | Potvrzuje `net10.0`, EF Core, PostgreSQL a řadu vazeb na sdílené projekty. Samotné vykopírování adresáře IS nestačí k samostatnému sestavení. |
| [Program.cs služby IS](/Users/jakubbednar/Developer/city-plus/src/Services/IS/src/Mesto.Plus.IS.Api/Program.cs) | Výchozí registrace API, databáze, logování a klientů. Má závislosti na DS, US, SRS a NFS; ty je nutné v samostatném produktu nahradit moduly nebo adaptéry. |
| [Issue.cs](/Users/jakubbednar/Developer/city-plus/src/Services/IS/src/Mesto.Plus.IS.Api/Domain/Entities/Issue.cs) | Obsahuje oznamovatele, polohu, operátora, přílohy, historii a stav. Vyžaduje polohu a obsahuje městské a komunitní vlastnosti; pro Krizovku upravit model a odstranit nepotřebné chování. |
| [IssueReport.cs](/Users/jakubbednar/Developer/city-plus/src/Services/IS/src/Mesto.Plus.IS.Api/Domain/Entities/IssueReport.cs) | V tomto zdroji znamená upozornění na nesprávný nebo nevhodný podnět. Není to samostatné krizové hlášení oznamovatele; nepřevzít jeho význam pouze podle názvu. |
| [IssuesController.cs](/Users/jakubbednar/Developer/city-plus/src/Services/IS/src/Mesto.Plus.IS.Api/Controllers/IssuesController.cs) | Existují operace přiřazení, interních poznámek, příloh, slučování a stavů. Část veřejných a anonymních cest neodpovídá identifikovanému internímu hlášení. |
| [UploadDocumentCommandHandler.cs](/Users/jakubbednar/Developer/city-plus/src/Services/DS/src/Mesto.Plus.DS.Application/Mesto.Plus.DS.Application/Core/Documents/Commands/UploadDocument/UploadDocumentCommandHandler.cs) | Počítá SHA-256, ukládá soubor a eviduje metadata. Očekává seekovatelný stream a ověřuje účet přes US. Pro obnovitelný upload a místní správu účtů potřebuje úpravu. |
| [DependencyInjection.cs služby DS](/Users/jakubbednar/Developer/city-plus/src/Services/DS/src/Mesto.Plus.DS.Infrastructure/DependencyInjection.cs) | Registruje souborový nebo paměťový backend. Při neznámém nastavení padá do paměťové varianty; v produkci musí chybějící konfigurace způsobit chybu spuštění. |
| [IStorageBackend.cs](/Users/jakubbednar/Developer/city-plus/src/Services/DS/src/Mesto.Plus.DS.Domain/Mesto.Plus.DS.Domain/Interfaces/IStorageBackend.cs) | Vhodné východisko rozhraní úložiště. V přečtené infrastruktuře DS nebyl nalezen hotový S3 backend; samotná enum hodnota `S3` jeho implementaci nedokládá. |
| [Shared.Authentication](/Users/jakubbednar/Developer/city-plus/src/Shared/Common/Mesto.Plus.Shared.Authentication/Mesto.Plus.Shared.Authentication/AuthenticationExtensions.cs) | Zdroj vzorů oprávnění a aktuálního uživatele. Převzetí vyžaduje odstranění vazeb na města, systémy a službu US. |
| [MessagingExtensions.cs](/Users/jakubbednar/Developer/city-plus/src/Shared/Common/Mesto.Plus.Shared.Messaging/Mesto.Plus.Shared.Messaging/MessagingExtensions.cs) | Registruje MassTransit s RabbitMQ nebo paměťovou dopravou. Přečtená registrace nedokládá transakční outbox; nelze ji považovat za záruku doručení událostí po pádu procesu. |

Ve zdrojovém C# stromu Města+ nebyly při hledání `AddSignalR`, `MapHub` a dědění z `Hub` nalezeny registrace SignalR. Živý dohled navržený pro Krizovku je doplnění, nikoli ověřená hotová vlastnost tohoto základu.

## 4. Next.js: co skutečně převzít

| Zdrojový soubor nebo adresář | Zjištění a potřebná úprava |
| --- | --- |
| [package.json frontendu](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/package.json) | Next.js, React, Bootstrap, TanStack Query, TypeScript, Orval a Vitest. Zachovat potřebný základ a sjednotit verze při vytvoření samostatné aplikace. |
| [package.json shellu](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/apps/shell/package.json) | Shell závisí na `@mesto/core`, `@mesto/module-mestoplus` a `@mesto/module-transport` i na sestavovacím skriptu. Není to izolovaná šablona bez závislostí. |
| [AuthContext.tsx](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/packages/core/src/contexts/AuthContext.tsx) | Uživatel, dostupné systémy a oprávnění. Městský kontext nahradit organizačním a vždy vynucovat oprávnění i v backendu. |
| [orval-mutator.ts](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/packages/core/src/shared/network/orval-mutator.ts) | Generovaný klient jde přes Next proxy `/api/prepareRequest`. Pro Krizovku se navrhuje přímé same-origin volání .NET API; generický proxy kontrakt se nepřenáší automaticky. |
| [IssuesModule.tsx](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/packages/module-mestoplus/src/issues/IssuesModule.tsx) | Seznam a filtry podnětů, explicitní ruční obnovení. Doplnit živé aktualizace, obnovu po přerušení spojení, dohledový režim a skutečné potvrzení převzetí. |
| [IssueDetail.tsx](/Users/jakubbednar/Developer/city-plus/src/Apps/Frontend/packages/module-mestoplus/src/issues/components/IssueDetail.tsx) | Zdroj pro detail události; převést na nový model hlášení a jeho oprávnění. |

## 5. Další nalezené možnosti

| Projekt | Rozsah prověření | Závěr |
| --- | --- | --- |
| `OICT_Aplikace/zmente-to-flutter-export` | Manifest, Git revize a existence exportu. | Vedlejší snapshot; hlavním zdrojem bude běžný repozitář Změňte.to. |
| `hrob` | README, Flutter manifest, manifest webu a seznam vybraných backendových souborů. | Obsahuje .NET 10 a Flutter; web je React/Vite, proto nepokrývá požadovaný Next.js dohled. Není potřebný jako hlavní základ. |
| `MOS-ekosystem/idolka-app` a `pid-litacka-2.0-frontend` | Manifesty a struktura projektů. | Další Flutter základy, ale doménově jsou vzdálenější než existující aplikace pro hlášení. |
| `MOS-ekosystem/kompas-backoffice`, `back-office-admin`, `release-calendar` | Manifesty s Next.js. | Alternativy pro jednotlivé vzory. Hlubší audit nebyl proveden; přednost má administrace Města+ s existující správou podnětů. |

## 6. Pravidla převzetí

Přebírá se vybraný kód a související potřebné testy do samostatného projektu Krizovky. Před prvním importem se vytvoří seznam souborů, jejich revizí, závislostí a provedených změn. Převzaté generované klienty se následně regenerují podle vlastního OpenAPI; nebudou se ručně přepisovat cizí DTO.

V kořenech hlavních zkoumaných zdrojů nebyl nalezen licenční soubor. To samo neurčuje právo k jejich použití; před distribucí se ověří vlastnictví a oprávnění k zamýšlenému převzetí. Podmínky původního HTML jsou uvedeny v produktovém záměru.

Nepřebírají se produkční přístupové údaje, `.env`, osobní data, databázové dumpy, podpisové certifikáty aplikací, cizí Firebase projekty, zákaznické adresy ani konfigurace nasazení. Nová aplikace dostane vlastní identifikátory, prostředí a tajné údaje. Před kopírováním se zkontrolují také potřebné licenční a autorské poznámky.

## 7. Doplnění: živé audio a video

Po doplnění požadavku na přímý přenos byly prohledány manifesty `pubspec.yaml`, `package.json` a `.csproj` v adresáři `Developer` na závislosti LiveKit, Flutter WebRTC, mediasoup a Janus. Žádná deklarace těchto závislostí nebyla nalezena; tento průzkum tedy nedoložil hotový lokální základ streamování.

Pro doplnění se navrhuje existující **LiveKit** a jeho [Flutter SDK](https://github.com/livekit/client-sdk-flutter) a [React komponenty](https://github.com/livekit/components-js). Jde o nový navržený základ, který se zatím neimportoval ani provozně neověřoval. Implementace použije publikování mikrofonu nebo kamery a příjem ve velínu, bez agentních či AI funkcí. Konkrétní propojení s .NET, oprávnění a provoz služby popisuje technický záměr.

## 8. Doplnění: organizace, knihovna a Nastavení

Více organizací s oddělenými daty je povinný rozsah první verze. Existující městské filtry a oprávnění jsou zdroj použitelných vzorů; tento průzkum nepotvrdil hotové oddělení celé Krizovky včetně souborů a streamů. To se musí ověřit napříč všemi převzatými částmi.

Společná knihovna metodik, převzetí do vlastní organizační kopie a zobrazení manuálu se doplní jako jednoduché funkce Krizovky. Pro ukládání mobilního kontaktního profilu lze vyjít ze stávajícího `flutter_secure_storage` ve Flutter základu. Chráněná webová profilová cookie bude nová malá integrace ASP.NET Core Data Protection; nenahrazuje autentizaci ani ověřování členství v organizaci.


## Doplnění při inicializaci – 17. 10. 2026

Na základě následného zadání byla převzata vlastní kopie služby US z City+ včetně potřebných knihoven. Kopie používá obory názvů `Krizovka.Users` a `Krizovka.Shared`, vlastní databázi a vlastní konfiguraci. Běžící služba City+ se nepoužívá. Z IS byly převzaty a zjednodušeny entity hlášení, příloh a historie; z architektonických testů kontroly domény a oddělení služeb. Podrobnosti jsou v [poznámkách k převzetí](prevzaty-kod.md) a souborový záznam původu v [reused-sources.json](reused-sources.json). Původní inventář výše zůstává záznamem tehdejšího průzkumu.

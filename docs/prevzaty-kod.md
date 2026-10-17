# Převzatý kód

Stav inicializace: 17. 10. 2026. Zdrojové repozitáře zůstávají beze změny. Seznam souborů s původními SHA-256 a revizemi je v [reused-sources.json](reused-sources.json). Záznamy se vztahují k souborům před úpravami, nikoliv ke kontrolním součtům výsledku.

## Uživatelé a oprávnění

Z City+ byly převzaty čtyři projekty US a jejich nezbytné projektové závislosti. Vnitřní struktura zůstává zachovaná, aby převzetí neznamenalo přepis celé služby. C# obory názvů jsou `Krizovka.Users` a `Krizovka.Shared`; některé názvy pomocných tříd stále zachovávají původ pro snazší dohledání.

Služba má **vlastní nasazení, vlastní databázi, vlastní podpisový klíč a vlastní interní API klíč**. Nepoužívá běžící City+ US, jeho uživatele ani oprávnění. Nebyly kopírovány zdrojové konfigurace, přístupové údaje, logy, seedovací balíčky ani produkční data.

Původní automatický demo účet a endpoint pro jeho vytvoření byly odstraněné. Převzaté přihlašování do velínu používá heslový hash BCrypt a podpis JWT; uživatelské účty se během inicializace nevytvářejí. Přihlášení klientských aplikací a první správa organizací se teprve zapojí.

Převzaté části pro mobilní identity počítají s Kratos. V tomto základu nejsou aktivní (`EndUserIdentity:Enabled` je ve výchozím stavu false); vlastní poskytovatel identit není nasazený. Převzaté backoffice přihlášení Kratos nepotřebuje. Před zapnutím registrace oznamovatelů je nutné její tok dokončit a ověřit.

Z převzatého startu byly odstraněné dynamické sidecary, automatické cizí seedery, napojení na centrální audit a registry. MassTransit zůstal jako interní závislost služby v paměťovém režimu; RabbitMQ se nenasazuje a zprávy nejsou trvalou frontou. Staré role související s městskými funkcemi nejsou novým produktovým požadavkem; při zapojení oprávnění je nahradí role uživatel, operátor a správce organizace.

Původní migrace neměly úplná EF metadata a snapshot. Vlastní počáteční migrace byly proto vytvořené z převzatého modelu pro **nové prázdné databáze Krizovky**. Neslouží pro migraci běžícího City+.

Při převzetí bylo odstraněné logování bearer tokenů, tokeny v klíčích paměťové cache byly nahrazené SHA-256 a webhooky bez nakonfigurovaného tajného klíče přístup odmítají. To nenahrazuje pozdější ověření oprávnění jednotlivých organizací a dokončení webové relace včetně CSRF ochrany.

## Hlášení

Z City+ **IS (Issue Service)** byly převzaty entity `Issue`, `IssueAttachment` a `IssueHistory`. Jsou součástí hlavního API, nevzniká další mikroslužba. Model byl upraven na organizaci, klientské ID proti duplicitám, volitelnou polohu s časem a přesností, kontaktní snímek a stavy nové/převzaté/uzavřené.

Povinná městská lokalita, hlasování, městské tagy, životní cyklus komunitních podnětů a vazby na další služby City+ se nepřebírají. U příloh se ukládá interní klíč souboru, nikoliv veřejná URL. Datový model a migrace jsou připravené; příjem hlášení a autorizované čtení souborů zatím nejsou implementované.

## Frontendy a testy

Next.js přebírá `ModulePage`, `PageHeader`, typy chyb API a pomocnou funkci pro výpis položek. Flutter přebírá síťové chyby, rušení požadavků a práci se zdrojem obrázku ze `zmente-to-flutter`; navigace a obrazovky jsou upravené pro Krizovku. Celá původní aplikace ani její přihlášení do Lítačky se nekopírují.

Architektonické testy jsou upravené ze City+ `DomainRulesTests` a `ServiceIsolationTests`. Kontrolují explicitně načtené a neprázdné sestavení uživatelské domény a oddělení obou backendů; nevyžadují sestavení všech služeb City+. Další čtyři testovací případy ověřují odmítnutí neautorizovaných webhooků.

Původní HTML s metodikami zůstává podkladem. Obsah se zatím automaticky neimportuje do komerční aplikace; jeho licenční omezení je popsané v produktovém záměru.

# Produktový záměr: Krizovka

**Verze:** 0.4 – více organizací, knihovna metodik a nastavení  
**Datum:** 15. 10. 2026  
**Platformy:** Flutter pro uživatelský web, iOS a Android; Next.js administrace; backend .NET 10  
**Podklad:** [Program-A-krizove-situace-VS.html](Program-A-krizove-situace-VS.html)  
**Navazuje:** [technický záměr](technicka-dokumentace.md) a [inventář existujících aplikací](docs/inventar-existujicich-aplikaci.md)

## 1. Hlavní účel

**Jsem v nebezpečí. Potřebuji rychle a tiše říct „tady se něco děje, pomoc“ a automaticky poslat své kontakty a dostupnou polohu.** To je hlavní úkol aplikace a měřítko pro rozhodování o každé další funkci.

Organizace, která si aplikaci platí, dostane hlášení do své administrace. Obsluha uvidí, kdo pomoc potřebuje, jak ho kontaktovat, co oznámil a kde byl při hlášení. Uživatel může poslat text, hlasovku nebo spustit živý přenos zvuku či videa do velínu. Druhou částí je manuál, jak se zachovat, s vlastními metodikami organizace. Třetí část představuje Nastavení, kde si uživatel uloží své údaje.

Aplikace je **multi-tenant**: jedno řešení obsluhuje více organizací, každou s vlastními hlášeními, obsluhou a metodikami. Společná knihovna poskytuje výchozí metodiky, které si organizace mohou převzít a přizpůsobit.

Cílem je rychlé zavedení jednoduché aplikace s využitím existujících základů. Vlastní vývoj doplní potřebné krizové chování a propojení částí.

## 2. Hlášení, metodiky a nastavení

Hlavní záložky jsou **Hlášení** a **Metodiky**. **Nastavení** tvoří třetí část dostupnou přes ikonu; nemusí zabírat místo mezi krizovými akcemi.

### Hlášení – výchozí obrazovka

Velké tlačítko **POTŘEBUJI POMOC** ihned odešle základní hlášení. Uživatel nemusí vybírat kategorii, vypisovat kontakty, čekat na GPS nebo dokončit přílohu.

Automaticky se připojí:

- Jméno a příjmení.
- Osobní, zaměstnanecké nebo studijní číslo.
- Telefon a e-mail.
- Organizace a čas hlášení.
- Dostupná poloha s časem a přesností měření.

Uživatel má na stejné záložce tyto možnosti:

| Možnost | Výsledek |
| --- | --- |
| **Text** | Pošle zprávu, například „střílí se tady“. |
| **Hlasovka** | Nahraje a odešle hlasovou zprávu, kterou si obsluha může přehrát i později. |
| **Živě audio** | Spustí přímý přenos mikrofonu do velínu, bez obrazu. |
| **Živě video** | Spustí přímý přenos kamery a mikrofonu do velínu. |

Spuštění nahrávání či streamu zároveň odešle základní upozornění s kontakty a dostupnou polohou. Na dokončení hlasovky nebo navázání přenosu se nečeká. Fotografie a uložený videoklip zůstávají doplňujícími přílohami.

Živý přenos může začít vysílat ihned po připojení, bez čekání na přijetí hovoru. Operátor ho otevře u hlášení. Výchozí přenos je jednosměrný do velínu, aby telefon uživatele nezačal přehrávat hlas operátora. Uživatel ho může kdykoliv ukončit; vidí, zda vysílá, zda se přenos přerušil a zda je velín připojen. Samotné vysílání není potvrzením, že někdo poslouchá.

Hlasovka se uloží jako příloha. Živý přenos se podle výchozího návrhu nearchivuje; požadavek na jeho záznam lze samostatně doplnit. Bez připojené obsluhy se odvysílaný obsah zpětně nepřehraje. Selhání streamu nezruší doručené základní hlášení a lze pokračovat textem nebo hlasovkou.

Přihlášení, příslušnost k organizaci, kontakty a oprávnění zařízení se nastaví předem. Pracovní označení „digitální podpis“ znamená přiřazení hlášení k přihlášenému účtu a přiložení vyplněných údajů. Samotné údaje z Nastavení nejsou ověřením pravdivosti jména, telefonu nebo osobního čísla. V první verzi se nepřidává další podpisový krok ani kryptografické podepisování.

### Metodiky

**Manuál „Jak se zachovat“ pro vlastní organizaci.** Uživatel si vybere nebo vyhledá situaci a zobrazí se mu stručné očíslované první kroky, poté doplňující pokyny a místní kontakty. Role lze nastavit předem, aby se v krizi znovu nevybíraly. Zobrazují se jen metodiky zveřejněné aktivní organizací, nikoli nevybraný obsah celé společné knihovny.

**V uživatelské aplikaci teď nebude mapa, plán budovy ani navigace k nouzovým východům.**

### Nastavení

Uživatel vyplní **jméno, příjmení, osobní číslo, telefon a e-mail**. Údaje se uloží do telefonu, případně na webu do chráněné cookie, a při hlášení se automaticky připojí. Může je později změnit nebo z tohoto zařízení vymazat. Do již doručených hlášení se změny zpětně nepromítají.

Při prvním spuštění aplikace (onboarding) je uživatel srozumitelně seznámen s účelem systému Krizovka a vyzván k úvodnímu vyplnění kontaktního profilu. Onboarding vysvětluje, že údaje slouží operátorům velínu pro rychlou identifikaci v nouzi a jsou uloženy výhradně lokálně v zařízení bez centrálního registru (GDPR minimalizace). Uživatel může profil vyplnit ihned nebo krok odložit na později.

Profil je uložený pro konkrétní zařízení nebo prohlížeč; na novém zařízení se vyplní znovu. Přihlašovací oprávnění a kontaktní profil jsou oddělené věci. V Nastavení je vidět aktivní organizace a připravenost oprávnění kamery, mikrofonu a polohy. Má-li uživatel více členství, organizaci zvolí předem, aby ji nemusel vybírat při incidentu.

## 3. Rychlost, poloha a tichost

Stisknutí pomoci má vést rovnou k odeslání. Pokud poloha ještě není připravená, hlášení odejde okamžitě a poloha se doplní, jakmile je k dispozici. Chybějící poloha musí být u příjemce zřetelná; starší měření se nesmí tvářit jako nové. Uvnitř budovy lze krátkým textem doplnit místnost nebo podlaží.

Aplikace sama nevydává zvuky, nevibruje, nespouští blesk a nepřehrává přílohy ani audio z velínu. Živý přenos nemá na telefonu vyzvánění či zvuk připojení. Kamera a mikrofon se aktivují vědomou volbou. Tiché chování se musí ověřit na skutečných zařízeních, protože systémové dialogy, indikátory a některé zvuky nemusí aplikace ovládat.

Uživatel vidí rozdíl mezi odesíláním, doručením a převzetím operátorem. Při výpadku spojení se ukáže neúspěšné či nepotvrzené odeslání a možnost opakovat pokus. Opakovaný pokus nesmí vytvořit duplicitní hlášení. První verze negarantuje doručení po zavření aplikace.

## 4. Administrace organizace

Administrace má dvě základní sekce:

| Sekce | První verze |
| --- | --- |
| Krizová hlášení | Automaticky obnovovaný seznam, detail, kontakty, hlasovky a přílohy, příjem živého audia/videa, mapa místa hlášení a potvrzení převzetí. |
| Metodiky | Vlastní postupy organizace, převzetí ze společné knihovny, jednoduchá úprava a zveřejnění. |

Mapa je součástí dohledu: obsluha vybere hlášení a uvidí na ní jeho polohu. Hlášení bez souřadnic se stále zobrazí v seznamu. Bod na mapě ukazuje místo při oznámení; nejde o průběžné sledování člověka. Plány podlaží a únikové cesty nejsou nyní součástí produktu.

Operátor mění pouze základní stavy **Nové → Převzaté → Uzavřené**. Převzetí má uvedeného operátora a čas. U živého hlášení otevře přenos přímo v detailu; poslech spouští vědomě. Mikrofon a kamera velínu se neaktivují. Při výpadku aktualizace přehled zřetelně ukáže, že data mohou být zastaralá, a při přerušení streamu ukáže jeho skutečný stav.

### Knihovna metodik a převzetí do organizace

Provozovatel udržuje společnou knihovnu výchozích metodik. Správce organizace si vybere šablonu a použije **Převzít do organizace**. Vznikne vlastní kopie, kterou může upravit, doplnit místními kontakty a zveřejnit v manuálu svých uživatelů. Organizace může tvořit i zcela vlastní metodiky.

Každá organizace spravuje vlastní obsah. Změna společné šablony automaticky nepřepíše převzaté kopie ani místní úpravy. Dodané HTML a jeho 20 situací představují podklad pro první knihovnu po vyřešení oprávnění k použití.

## 5. Uživatelé a organizace

Postačí tři aplikační role:

- **Uživatel:** odešle vlastní hlášení, čte manuál své organizace a spravuje lokální údaje v Nastavení.
- **Operátor:** přijímá a řeší hlášení své organizace.
- **Správce:** spravuje členství a metodiky své organizace včetně převzetí z knihovny.

Každá organizace vidí pouze svá hlášení, přílohy, streamy a vlastní metodiky. Společná knihovna je zdroj šablon; není společným úložištěm zákaznických hlášení. Uživatel pracuje v aktivní organizaci, do které má oprávněné členství. Přepnutí organizace změní manuál i příjemce nových hlášení, nikoli příjemce již vzniklého hlášení.

Osobní údaje vyplní uživatel v Nastavení a má je připravené v zařízení. Organizace tak nemusí předem budovat centrální registr kontaktních profilů; u přijatého hlášení se jejich kopie uloží. Správa společné knihovny je oprávnění provozovatele a sama nezakládá přístup k hlášením organizací.

Organizace určí, kdo příjem obsluhuje a kdy. Doručení do systému není automatické přivolání policie nebo jiných složek IZS. Tísňové a místní kontakty mají být snadno dostupné.

## 6. Využití existujících aplikací

| Část | Vybraný základ |
| --- | --- |
| Flutter | Změňte.to: použitelné přihlášení, hlášení, fotografie a poloha. |
| .NET 10 | Vybrané části služeb hlášení a souborů z Města+. |
| Next.js | Základ administrace, seznamu, detailu a mapy z Města+. |
| Živé audio/video | Hotová mediální služba a klientská SDK; technický návrh počítá s LiveKitem. |
| Obsah | Struktura 20 situací z dodaného HTML po vyřešení podmínek použití. |

Přebírají se potřebné části. Konkrétní soubory, revize a omezení popisuje inventář; jeho širší průzkum neznamená povinnost všechny nalezené moduly použít.

## 7. Co musí zvládnout první verze

1. Připravený uživatel jedním stisknutím odešle základní pomoc s automatickými kontakty a dostupnou polohou.
2. Organizace hlášení během několika sekund vidí v seznamu a při známých souřadnicích na mapě.
3. Operátor otevře detail a potvrdí převzetí.
4. Uživatel může poslat text či hlasovku a spustit přímý živý audio nebo video přenos do velínu bez zdržení prvního upozornění. Fotografie a uložené video zůstávají dostupné.
5. Druhá záložka zobrazí manuál aktivní organizace. Správce může tvořit vlastní metodiky nebo převzít kopie ze společné knihovny a upravit je.
6. Chyba sítě, zamítnutá poloha nebo neúspěšná příloha nevyvolají falešné potvrzení doručení.
7. Nastavení uchová kontaktní údaje v telefonu či chráněné webové cookie a automaticky je připojí ke hlášení.
8. Více organizací používá aplikaci současně, s oddělenými hlášeními, přenosy a vlastními metodikami.

První ověření proběhne při domluveném cvičení na reálném iPhonu, Androidu a webu. Ověří se text, hlasovka, živé audio i video, rychlost, kontakty, poloha, tichost, opakování bez duplicity a oddělení dat dvou organizací. U přenosu také nepřipojený velín, výpadek sítě, obnovení a ukončení. U metodik převzetí šablony a nezávislá úprava vlastní kopie; u Nastavení uložení, změna a vymazání údajů i přepnutí účtu.

## 8. Co teď odkládáme

Uživatelské mapy, plány budov, navigaci a únikové trasy, archivaci živého přenosu, automatické doručování po zavření aplikace, plný offline režim, slučování incidentů, chat, hromadné varování a složité schvalování. Text, hlasovka a živé audio/video jsou součástí první verze.

## 9. Podmínky použití podkladu

HTML uvádí autora **Radka Kuklu, 2026**, verzi 1.0 a datum ověření 23. 9. 2026. Povoluje volné šíření mezi školami pro nekomerční vzdělávací účely v nezměněné podobě s uvedením autora; úpravy požaduje konzultovat. Před převzetím do placené aplikace je potřeba vyjasnit zamýšlené použití. Stejně se ověří oprávnění k převzetí zdrojových aplikací.

Při spuštění organizace určí správce metodik, ověří své kontakty a stanoví dobu uchování hlášení a příloh. Podrobnosti implementace uvádí samostatný technický záměr.

## Příloha: Situace v dodaném HTML

1. Ozbrojený útok.
2. Oznámení o uložení výbušniny (NVS).
3. Nález podezřelého předmětu.
4. Podezřelá zásilka.
5. Braní rukojmí.
6. Požár a žhářství.
7. Havárie v laboratoři.
8. Vážné zranění, zdravotní příhoda, úmrtí.
9. Psychická krize a ohrožení sebe sama.
10. Fyzické napadení.
11. Verbální agrese a výhrůžky.
12. Podezření na zbraň.
13. Nebezpečné pronásledování (stalking).
14. Sexuální obtěžování a nevhodné chování.
15. Návykové látky.
16. Neoprávněný vstup do neveřejných prostor.
17. Kybernetický incident, únik dat.
18. Krádež a poškození majetku.
19. Demonstrace, obsazení budovy, nepokoje.
20. Sledování objektu, drony.

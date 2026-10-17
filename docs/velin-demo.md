# Správa uživatelů, organizací a ukázková data

Velín obsahuje čtyři části: **Hlášení**, **Metodiky**, **Uživatelé** a **Organizace**. Organizace odpovídá tenantovi. Přepínač v záhlaví mění obsah hlášení, metodik a uživatelů; seznam organizací je přehledem pro ukázkového správce platformy.

## Převzetí z City+

- `TableCustom`, `StatusBadge` a `ConfirmModal` pocházejí ze sdílených komponent City+.
- Seznam uživatelů vychází z `access-control/components/AdminList.tsx`.
- Seznam organizací vychází z `providers/components/ProvidersList.tsx`.
- Původní vazby na generované klienty, městský registr a globální superadmin role jsou nahrazené potřebami Krizovky. Formuláře používají stávající React Bootstrap.
- Zdrojové revize a původní kontrolní součty jsou doplněné v [reused-sources.json](reused-sources.json).

## Co lze vyzkoušet

1. Přidat a upravit organizaci: název, unikátní kód, kontaktní e-mail a stav.
2. Přidat a upravit uživatele: jméno, příjmení, e-mail, ukázkové kontaktní údaje, aktivní/neaktivní stav a členství.
3. Přidělit jednomu uživateli různé role v různých organizacích: uživatel, operátor nebo správce.
4. Hledat a filtrovat uživatele podle role a stavu; organizace podle stavu.
5. Přepínat organizaci, otevřít detail ukázkového hlášení a převzít nebo uzavřít hlášení v demo stavu.
6. Prohlížet ukázkové karty metodik. Odborný obsah a import z knihovny ještě nejsou připojené.
7. Obnovit celou výchozí sadu tlačítkem **Obnovit ukázková data**.

Výchozí sada obsahuje **3 organizace, 7 osob, 8 členství, 6 hlášení a 5 metodik**. Jedna osoba má různé role ve dvou organizacích. Jedna organizace je připravovaná a nemá hlášení ani metodiky. Jeden uživatel je neaktivní. Všechny e-maily používají doménu `.example.invalid`; telefonní čísla ani skutečná média se negenerují.

## Uložení a hranice dema

Data a jejich změny se ukládají do `localStorage` pod klíčem `krizovka.admin.demo.v1`. Přežijí obnovení stránky, zůstávají v konkrétním prohlížeči a mezi jeho kartami se aktualizují. Tlačítko obnovy nahradí místní změny výchozí sadou. Bez dostupného úložiště zůstávají změny pouze v paměti a aplikace zobrazí upozornění.

Tento krok je **interaktivní mock velínu**. Nevytváří skutečné účty, neodesílá pozvánky a nezapisuje do PostgreSQL ani US. Osobní číslo a telefon v demo editoru nepředstavují dokončený kontaktní profil oznamovatele uložený v jeho zařízení. Přepnutí organizace je zatím filtrování ukázkových záznamů, nikoliv zabezpečená autorizace; tu nad skutečnými daty musí provést backend.

Hlášení s typem audio/video jsou fiktivní záznamy bez nahrávky nebo přenosu. Mapa a skutečný příjem hlášení se teprve připojí. Zdrojové City+ a jeho produkční data zůstávají beze změny.

## Ověření

Prošlo `npm run check:admin` a produkční sestavení. V prohlížeči bylo vyzkoušené vytvoření organizace a uživatele, zachování změn po obnovení stránky, více členství, odlišné role, deaktivace, vyhledávání, odmítnutí duplicitního kódu, přepínání dat organizací, převzetí ukázkového hlášení, prázdný stav metodik a obnova dat. Rozložení bylo zkontrolované v oknech širokých 375 px a 812 px a na desktopu.

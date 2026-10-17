# Ověření inicializace

Datum: 17. 10. 2026. Ověřovaný stav je vývojový základ popsaný v [README](../README.md), nikoliv hotový krizový provoz.

| Kontrola | Výsledek |
| --- | --- |
| `dotnet test backend/Krizovka.slnx` | 9 architektonických a 4 bezpečnostní případy prošly. |
| EF kontrola změn modelu oproti migracím | Bez nezachycených změn u hlavního API i Users. |
| `npm run check:admin` | TypeScript a ESLint bez chyb a upozornění. |
| `npm run build:admin` | Úspěšný produkční build Next.js. |
| `flutter analyze` | Bez nálezů. |
| `flutter test` | 2 widget testy prošly: navigace, neaktivní pomoc a malá obrazovka se zvětšeným textem. |
| `flutter build web --release` | Úspěšný build; Flutter vydal upozornění na nenalezenou nepoužívanou rodinu CupertinoIcons. |
| Docker obrazy na serveru amd64 | API, Users, Next.js a Flutter web sestavené a načtené do Minikube. |
| Kubernetes | Všech 5 podů Running a Ready, PVC 4 GiB Bound. |
| HTTP kontroly v clusteru | Všech 15 kontrol v `scripts/cluster-smoke.mjs` prošlo. |
| Databázové migrace | Aplikované `InitialReports` a `InitialUsers`, potvrzené v obou migračních historiích. |
| Databázová oprávnění | API role nemá CONNECT do Users DB; Users role nemá CONNECT do hlavní DB. |
| Výchozí data | 0 hlášení, 0 backoffice uživatelů, 0 uživatelských účtů. |
| Náhled přes SSH | Dostupný Flutter na portu 3080 a Next.js na 3100; obě výchozí obrazovky zkontrolované také v prohlížeči. |
| Git | Secrets, buildy a lokální závislosti jsou ignorované; kontrola původu převzatých souborů prošla. |

HTTP kontroly zahrnují oba databázové readiness endpointy, dostupnost frontendů, přímý odkaz Flutteru, oba frontendové proxy vstupy, odmítnutí anonymního a neplatného přihlášení, ochranu kontextového API interním klíčem, ochranu registračního webhooku a odmítnutí neexistujícího účtu.

Opakování provozní kontroly z kořene repozitáře:

```sh
ssh kuba@192.168.1.38 \
  'kubectl -n krizovka exec -i deployment/krizovka-admin -- node --input-type=module' \
  < scripts/cluster-smoke.mjs
```

Skutečné přihlášení uživatele, izolace dvou organizací na aplikační úrovni, krizové odeslání, mikrofon, kamera, poloha, živé přenosy, lokální profil, metodiky a nativní běh na telefonech se ověří při jejich implementaci. Aktuální 401 testy ověřují odmítnutí přístupu; neprokazují dokončené přihlašování a správu oprávnění. Veřejný ingress, TLS, zálohy a LiveKit zatím nejsou nasazené.

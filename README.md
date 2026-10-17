# Krizovka

Jeden repozitář pro Flutter (web, iOS, Android), .NET 10 API, vlastní uživatelskou službu převzatou z City+ a Next.js velín. Úložiště je PostgreSQL.

Toto je **vývojový základ**, nikoliv hotový příjem krizových hlášení. Uživatelské obrazovky mají navigaci a zřetelné upozornění; tlačítko pomoci je zatím neaktivní. Hlášení, metodiky, přihlášení v aplikacích, kontaktní profil, mapa a živé přenosy se připojí v dalších krocích. iOS a Android projekty jsou založené; běh na skutečných telefonech ještě není ověřený.

## Struktura

| Cesta | Obsah |
| --- | --- |
| `apps/client` | Flutter, Riverpod, GoRouter, převzaté síťové a chybové utility. |
| `apps/admin` | Next.js velín, převzaté komponenty seznamů a chybových stavů. |
| `backend/Krizovka.Api` | Hlavní API; upravené entity hlášení, příloh a historie z City+ IS a vlastní migrace. |
| `backend/recycled` | Vlastní kopie City+ US a nezbytných knihoven, obory názvů `Krizovka.Users` a `Krizovka.Shared`. |
| `backend/Tests` | Převzaté a zúžené architektonické testy; testy ochrany webhooků. |
| `infra/k8s` | Namespace, PostgreSQL, aplikace a interní služby. |
| `docs/reused-sources.json` | Původ převzatých souborů, revize a kontrolní součty před úpravami. |

Produktové chování popisuje [produktový záměr](produktovy-zamer.md), technický návrh [technická dokumentace](technicka-dokumentace.md). Rozsah převzetí je v [poznámkách k převzatému kódu](docs/prevzaty-kod.md).

## Kontroly a místní vývoj

Použijte .NET 10 SDK podle `global.json`, Node 22 podle `.nvmrc` a Flutter podle `.fvmrc`. Flutter příkazy lze spouštět přes `fvm flutter`.

```sh
dotnet tool restore
dotnet test backend/Krizovka.slnx
npm ci
npm run check:admin
npm run build:admin
cd apps/client
fvm flutter pub get
fvm flutter analyze
fvm flutter test
fvm flutter build web --release
```

Flutter lze spustit pomocí `fvm flutter run -d chrome`, velín z kořene přes `npm run dev:admin` (port 3100). API vyžaduje přístup k vlastní databázi a nastavené proměnné z `.env.example`; příklad se automaticky nenačítá. Po nastavení prostředí spusťte `dotnet run --project backend/Krizovka.Api --no-launch-profile --urls http://127.0.0.1:5080`. Uživatelská služba má samostatný projekt v `backend/recycled/src/Services/US/src/Krizovka.Users.Api` a vlastní připojení `DB_CONNECTION_STRING`.

## Vývojový cluster

Server: `kuba@192.168.1.38`, Minikube, namespace **`krizovka`**. Všechny služby jsou interní `ClusterIP`; veřejná doména ani ingress zatím nejsou nastavené. City+ se používá jako zdroj kódu a sdílí pouze Kubernetes infrastrukturu.

PostgreSQL má 4 GiB PVC a dvě databáze s různými vlastníky: `krizovka` pro hlášení a `krizovka_users` pro účty a oprávnění. Inicializační skript vytváří role při prvním spuštění prázdného disku. Změna Kubernetes Secret sama o sobě nemění hesla existujících PostgreSQL rolí.

Pro náhled spusťte z kořene:

```sh
bash scripts/open-cluster-preview.sh
```

Flutter se otevře na `http://127.0.0.1:3080`, velín na `http://127.0.0.1:3100`. Tunel je aktivní do Ctrl+C. Toto připojení slouží pro vývojový náhled; ověření kamery, mikrofonu a polohy na telefonech vyžaduje následně HTTPS a skutečná zařízení.

Stav a logy:

```sh
ssh kuba@192.168.1.38 'kubectl -n krizovka get pods,svc,pvc'
ssh kuba@192.168.1.38 'kubectl -n krizovka logs deployment/krizovka-api --tail=50'
```

Přístupové údaje jsou samostatně v Kubernetes Secrets. Místní záložní soubor `.local/k8s-secrets.json` je ignorovaný Gitem a má práva 0600. `python3 scripts/create-local-secrets.py` vytvoří nové hodnoty pouze při neexistujícím souboru; při opakování staré zachová. Při ztrátě souboru na existujícím clusteru nejprve obnovte stávající secrets. Nevytvářejte náhradní hesla bez jejich změny také v databázi.

Ruční sestavení a nasazení je popsané v [provozním postupu](docs/nasazeni.md). Lokální obrazy mají `imagePullPolicy: Never` a musí být načtené do Minikube. Není potřeba registry ani Git remote; odkaz na Git doplníme později.

## Další krok

Připojit přihlášení a členství v organizaci, pak dokončit řetězec **POMOC → databáze → velín → mapa → převzetí**. Každý přístup musí ověřovat organizaci. Následují kontaktní profil, hlasovka a živé audio/video, organizační metodiky a jejich knihovna. Aktuální datový základ obsahuje `OrganizationId`; úplná izolace organizací ještě není implementovaná a otestovaná.

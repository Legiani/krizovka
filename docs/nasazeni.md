# Nasazení vývojového základu

Server `kuba@192.168.1.38` má dostupné Docker, Minikube a kubectl. Pracovní kopie Krizovky je v `/home/kuba/krizovka`. Všechny níže uvedené změny se týkají pouze namespace `krizovka`; City+ má vlastní namespace a data.

## První vytvoření prostředí

Přístupové údaje vzniknou lokálně; jejich hodnoty se nevypisují:

```sh
python3 scripts/create-local-secrets.py
ssh kuba@192.168.1.38 'kubectl apply -f -' < infra/k8s/namespace.yaml
ssh kuba@192.168.1.38 'kubectl apply -f -' < .local/k8s-secrets.json
ssh kuba@192.168.1.38 'kubectl -n krizovka apply -f -' < infra/k8s/postgres.yaml
```

Tento krok je pro prázdné prostředí. Při existujícím PostgreSQL disku zachovejte původní přístupové údaje; změna Secret nemění PostgreSQL hesla. PVC nemažte kvůli opakovanému nasazení.

## Sestavení a načtení obrazů

Nejprve sestavte Flutter web lokálně:

```sh
cd apps/client
fvm flutter pub get
fvm flutter build web --release
cd ../..
```

Přeneste zdroje bez vývojových závislostí a macOS resource forks:

```sh
ssh kuba@192.168.1.38 'mkdir -p /home/kuba/krizovka'
COPYFILE_DISABLE=1 tar --no-xattrs \
  --exclude=node_modules --exclude=.next --exclude=bin --exclude=obj \
  --exclude=.dart_tool --exclude=Pods --exclude=.fvm --exclude='._*' \
  -czf - backend apps infra scripts package.json package-lock.json .dockerignore \
  | ssh kuba@192.168.1.38 'tar -xzf - -C /home/kuba/krizovka'
ssh kuba@192.168.1.38 'cd /home/kuba/krizovka && bash scripts/build-images.sh'
```

Skript sestavuje pro architekturu serveru (amd64); klientský obraz pouze balí předem sestavený Flutter web. Logy sestavení jsou v `.local/build-*.log` na serveru. Při změně či smazání zdrojů v již přenesené kopii je také synchronizujte; samotný tar nepřepisuje adresář jako zrcadlo.

```sh
ssh kuba@192.168.1.38 'minikube image load \
  krizovka/api:bootstrap-20261017 krizovka/users:bootstrap-20261017 \
  krizovka/admin:demo-20261017-1 krizovka/client:bootstrap-20261017'
ssh kuba@192.168.1.38 'kubectl apply -k /home/kuba/krizovka/infra/k8s'
```

Při dalších verzích nastavte `KRIZOVKA_IMAGE_TAG` při sestavení a stejný nový tag ve čtyřech obrazech manifestu `applications.yaml`. Pro samostatné vydání velínu lze použít `KRIZOVKA_ADMIN_IMAGE_TAG` a změnit pouze jeho obraz. Neměnné tagy zabrání tomu, aby Kubernetes použil starý obraz z cache. Tag `bootstrap-20261017` identifikuje první inicializaci; `demo-20261017-1` přidává do velínu správu uživatelů, tenantů a ukázková data.

## Kontrola

```sh
ssh kuba@192.168.1.38 'kubectl -n krizovka get pods,svc,pvc'
ssh kuba@192.168.1.38 'kubectl -n krizovka rollout status deployment/krizovka-api --timeout=60s'
ssh kuba@192.168.1.38 'kubectl -n krizovka logs deployment/krizovka-users --tail=50'
```

API a Users mají `/health/live` a `/health/ready`; readiness kontroluje databázové spojení a proces startuje až po vlastních migracích. Flutter web a Next.js mají `/health/live`. Chráněné `/api/session` bez přihlášení vrací 401. Podrobný stav prvního ověření je v [ověření základu](overeni-zakladu.md).

Pro otevření obou frontendů použijte `bash scripts/open-cluster-preview.sh`. Veřejný ingress, domény, HTTPS, zálohy a LiveKit zatím součástí tohoto vývojového nasazení nejsou.

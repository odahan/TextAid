# TextAid

[English](README.md)

![Logo TextAid](assets/Logo-final.png)

**Transformation et traduction de texte pour Windows, avec un traitement local par défaut.**

TextAid capture le texte sélectionné dans une autre application Windows, lui applique une transformation par IA configurable, affiche le résultat et permet de remplacer la sélection d’origine, de copier le résultat ou d’annuler. Il utilise par défaut un modèle Ollama local ; le recours à un fournisseur distant relève d’un choix explicite de l’utilisateur.

## Aperçu de l’application

![TextAid traduisant le texte sélectionné](assets/Screenshots/02%20Call%20TextAid%20to%20translate.png)

## Fonctionnalités

- Capturez le texte sélectionné avec `Ctrl+C`, puis `C` pour choisir une action, ou `Ctrl+C`, puis `T` pour une traduction immédiate.
- Identifiez la langue capturée par un appel IA asynchrone sur les deux raccourcis, sans bloquer l’interface. La réponse est réutilisée tant que le texte ne change pas ; un texte ambigu demande une destination de traduction explicite.
- Reformulez, corrigez, traduisez, résumez, raccourcissez, développez, simplifiez, changez le ton, rendez le texte plus naturel ou créez des actions personnalisées déclaratives.
- Prévisualisez les résultats Markdown bruts ou mis en forme dans une interface Windows sombre.
- Conservez par défaut les échanges avec le modèle en local, avec une configuration explicite pour les fournisseurs sur le réseau interne ou externes.
- Exécutez l’application sous la forme d’un exécutable Windows x64 autonome ; la version publiée ne nécessite pas l’installation du runtime .NET.

## Prérequis

- Windows x64
- SDK .NET 10 pour compiler à partir des sources
- [Ollama](https://ollama.com/) et un modèle local compatible pour la configuration par défaut

## Compilation et tests

Dans PowerShell, depuis la racine du dépôt :

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) 'src\TextAid.App\obj\NuGetScratch'
New-Item -ItemType Directory -Path $env:NUGET_SCRATCH -Force | Out-Null
dotnet restore TextAid.sln -m:1 -p:NuGetAudit=false
dotnet build TextAid.sln -m:1 -p:NuGetAudit=false
dotnet test TextAid.sln -m:1 -p:NuGetAudit=false
```

Pour créer l’exécutable Release de référence en un seul fichier, lancez :

```powershell
.\publish.ps1
```

L’exécutable obtenu se trouve dans `src/TextAid.App/bin/Publish/TextAid.exe`.

Pour lancer l’application publiée depuis un environnement empaqueté comme Codex, utilisez `./start.ps1`. Cette commande passe par le bureau Windows afin que TextAid utilise la configuration utilisateur habituelle. Vous pouvez aussi lancer directement l’exécutable depuis l’Explorateur de fichiers.

## Méthode du projet

Ce dépôt est maintenu avec **SAW 3.2 (SDD Another Way)**, une méthode créée par Olivier Dahan © 2025–2026 qui prolonge Pro-Spec 3. La méthode repose directement sur Markdown et Git : elle ne dépend d’aucun outil externe.

L’intention du produit, les règles durables, les décisions, l’état du projet et les spécifications historiques restent dans le dépôt. Avant de modifier un lot planifié ou son état de validation, suivez la procédure d’entrée décrite dans [la référence SAW 3.2 / Pro-Spec 3](docs/PROSPEC-3-SPECIFICATION.md). Les sources françaises sont conservées à côté des documents traduits avec le suffixe `.FR.md`.

## Démarrage centré sur l’IA

Pour conduire ce projet avec une IA, utilisez cette instruction :

> **Lis le fichier `README.md` et exécute la prochaine étape.**

Les instructions de démarrage actives sont délibérément contenues dans ce README. Une IA doit d’abord lire l’intention du produit dans `PROJECT.md`, les contraintes durables dans `RULES.md`, toutes les décisions actives dans `LEDGER.md` (ainsi que les décisions obsolètes auxquelles elles font référence), puis le plan actuel dans `STATUS.md`. Elle doit ensuite lire les fichiers `SPEC`, `FINDINGS`, `GATES` et, s’il existe, `CONVERGENCE` du lot actif. Consultez `HISTORY.md` lorsque la chronologie facilite la reprise ou l’audit.

`STATUS.md` est la seule autorité concernant l’état des lots. Il désigne le prochain lot `Planned` et ses dépendances ; un seul lot peut être à l’état `In-progress`, `Ready-to-close` ou `Blocked` à la fois. Une décision humaine est nécessaire pour passer un lot à `In-progress`, le clôturer, valider une porte HUMAN, accepter une dérogation ou apporter toute autre modification significative du sens.

Pendant qu’un lot est actif, implémentez sa `SPEC`, consignez les connaissances dans `FINDINGS`, maintenez dans `STATUS.md` les informations permettant de retrouver le résultat, le travail restant, les blocages et la prochaine action, puis ajoutez les opérations significatives à `HISTORY.md`. Évaluez chaque porte de validation par rapport à un résultat identifiable. Un lot ne peut être clôturé que lorsque toutes les portes applicables sont valablement `PASS` ou `N/A`, que toutes les exigences et tous les constats ont été pris en compte et qu’un humain accepte la clôture. Mettez `STATUS.md` à jour en dernier.

> Si vous avez besoin d’une IA assez puissante pour gérer votre projet, et que le projet est assez vaste pour nécessiter une méthode, alors l’IA utilisée est capable de gérer toute la bureaucratie de cette méthode.

Les instructions de démarrage historiques complètes sont conservées sans modification dans [docs/README.BOOTSTRAP.md](docs/README.BOOTSTRAP.md). Les instructions actives ci-dessus remplacent uniquement leurs anciennes références à un outil Pro-Spec externe, que SAW 3.2 n’utilise plus.

## Validation de la méthode

TextAid a été créé pour répondre à son propre objectif et pour mettre SAW à l’épreuve. Son développement a utilisé uniquement GPT-5.6 Terra avec un effort de raisonnement Medium et un abonnement OpenAI à 20 USD. Le projet démontre que le Spec-Driven Development (SDD) de Microsoft peut être adapté en une méthode pratique entièrement centrée sur l’IA. SAW (SDD Another Way) est cette adaptation : une alternative à l’accompagnement proposé par Spec-Kit de Microsoft pour appliquer le SDD dans un projet réel.

## Documentation

- [Intention du produit](PROJECT.md)
- [État actuel du projet](STATUS.md)
- [Structure des packs de langue](docs/language-packs.md)
- [Cartographie des spécifications sources](docs/SOURCE-MAP.md)
- [Référence SAW 3.2 / Pro-Spec 3](docs/PROSPEC-3-SPECIFICATION.md)
- [README de démarrage d’origine](docs/README.BOOTSTRAP.md)

## Licence et attribution

Le code source de TextAid est disponible sous la [licence Creative Commons Attribution - Utilisation non commerciale 4.0 International](Licence.md). Vous pouvez le copier, le partager et l’adapter uniquement à des fins non commerciales. Toute distribution doit conserver l’attribution **Olivier Dahan © 2026** et le contact `odahan [at] e-naxos [dot] com`, indiquer les modifications et inclure la mention de licence. Toute utilisation commerciale nécessite l’accord écrit préalable de l’auteur.

Cette licence est volontairement non commerciale et n’est donc pas une licence Open Source approuvée par l’OSI.

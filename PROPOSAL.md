# Proposition d'Architecture : CSharpDocumentor

Cette proposition détaille une approche moderne et robuste pour l'automatisation de la documentation XML C# en utilisant l'analyse sémantique et l'intelligence artificielle.

## 1. Principes Directeurs
*   **Intégrité du Code :** L'IA ne doit jamais réécrire le code logique. Elle génère uniquement du texte (JSON) que l'outil injecte via Roslyn.
*   **Contexte Sémantique :** Une documentation de qualité nécessite de comprendre les relations entre les classes, les interfaces et l'usage des membres.
*   **Idempotence :** L'outil doit pouvoir être exécuté plusieurs fois sans dégrader ou dupliquer la documentation existante.
*   **Approche Locale :** Conçu comme un outil CLI pour le développeur, avec un contrôle total avant application.

---

## 2. Architecture Technique

L'application est découpée en quatre modules principaux :

### A. Scanner & Analyseur (Roslyn `MSBuildWorkspace`)
Plutôt qu'une simple lecture de fichiers texte, nous utilisons `MSBuildWorkspace` pour charger la solution/le projet.
*   **Avantage :** Permet de résoudre les types à travers différents fichiers (ex: connaître les membres d'une interface définie ailleurs).
*   **Fonction :** Identifie les classes, interfaces, records et membres publics/internes dépourvus de balise `<summary>`.

### B. Constructeur de Contexte (Context Builder)
Pour chaque classe à documenter, le module prépare une "vue" riche envoyée au LLM :
*   **Signature de classe :** Héritage et interfaces (permet au LLM de comprendre le contrat).
*   **Membres documentés :** Inclus dans le contexte pour cohérence de style, mais marqués comme "déjà documentés".
*   **Membres cibles :** Signatures complètes des membres sans documentation XML.
*   **Corps de méthodes :** Inclus mais tronqués (ex: max 50 lignes) pour donner l'intention sans saturer les tokens.
*   **Métadonnées :** Nom du projet, espace de noms racine.

### C. Générateur de Documentation (LLM Engine)
Le choix du modèle est flexible selon les besoins de l'utilisateur local :
*   **Claude 3.5 Sonnet (Recommandé) :** Meilleur rapport qualité/prix pour le code, respect strict du JSON.
*   **GPT-4o :** Alternative robuste.
*   **Modèles Locaux (Llama 3 / CodeLlama via Ollama) :** Option privilégiée pour la confidentialité totale des sources.
*   **Prompt Engineering :**
    *   *System Prompt :* Définit le rôle d'expert technique .NET et impose le format de sortie JSON.
    *   *User Prompt :* Fournit le contexte de la classe et la liste explicite des `MemberNames` à documenter.
    *   *JSON Schema :* Utilisation de `Response Format` (si supporté par le LLM) pour garantir la structure `{ "members": [ { "name": "...", "xml": "..." } ] }`.

### E. Optimisation pour LLM Local : L'Approche RAG
Pour les modèles locaux (souvent moins volumineux en paramètres que Claude 3.5), l'ajout d'une couche de **RAG (Retrieval-Augmented Generation)** est une stratégie industrielle forte :
*   **Base de connaissances "Frameworks" :** Injecter des fragments de documentation officielle (Clean Architecture, AutoMapper, FluentValidation) permet au LLM de produire des commentaires XML qui respectent la terminologie exacte de ces outils.
*   **Base de connaissances "Projet" :** Rechercher des exemples de documentation déjà existants dans le projet pour assurer une cohérence de style et de ton (Few-Shot Prompting dynamique).
*   **Implémentation :** Utilisation d'une base de vecteurs légère et locale (ex: SQLite avec extension vectorielle ou FAISS) pour ne pas dépendre d'un service cloud.

### D. Injecteur de Documentation (Roslyn `SyntaxRewriter`)
Utilise un `CSharpSyntaxRewriter` pour insérer les commentaires XML dans l'Arbre de Syntaxe Abstraite (AST).
*   **Précision :** Garantit que les commentaires sont placés exactement au bon endroit avec l'indentation correcte (utilisation de `SyntaxFactory.TriviaList` et `SyntaxFactory.Comment`).
*   **Logique de filtrage :** Avant injection, l'outil vérifie à nouveau si le membre possède des `DocumentationCommentTrivia`. Si oui, il ignore l'injection pour ce membre (protection contre les modifications concurrentes ou erreurs de logique).
*   **Sécurité :** Création systématique de fichiers `.bak` avant toute écriture sur le disque.

### F. Synthèse du Projet & Visualisation (Architecture-as-Code)
L'outil peut s'étendre à la génération de la documentation de haut niveau (README.md) :
*   **Analyseur Global :** Parcourt la solution complète pour extraire la structure des projets (couches), les dépendances inter-projets et les flux de données principaux.
*   **Générateur de Schémas (Mermaid.js) :** Traduit la structure sémantique en code Mermaid pour afficher des diagrammes d'architecture (Class Diagrams, Dependency Graphs) directement dans le Markdown.
*   **Rédacteur de README :** Utilise le LLM pour rédiger une présentation claire du projet (Purpose, Setup, Usage) basée sur l'analyse de tous les fichiers `.csproj` et des points d'entrée (Program.cs, Controllers).

---

## 3. Workflow de Fonctionnement

1.  **Initialisation :** Chargement du `.csproj` via `MSBuildLocator`.
2.  **Filtrage :** Parcours de l'AST pour trouver les membres non documentés.
3.  **Batching :** Regroupement par classe (1 appel API = 1 classe complète).
4.  **Appel LLM :**
    *   *Prompt système :* "Tu es un expert .NET. Retourne un JSON uniquement."
    *   *Prompt utilisateur :* Le code de la classe + liste des membres cibles.
5.  **Validation :** Vérification du JSON reçu.
6.  **Injection :** Transformation de l'AST et écriture sur disque.

---

## 4. Comparaison des Approches

| Caractéristique | Approche Naïve (Regex/Texte) | Notre Approche (Roslyn + LLM) |
| :--- | :--- | :--- |
| **Précision du placement** | Faible (risque de décalage) | Parfaite (AST-based) |
| **Compréhension** | Nulle (ligne par ligne) | Haute (Contexte de classe) |
| **Respect du code** | Risqué (peut corrompre) | Garanti (injection de trivia uniquement) |
| **Performance** | Rapide | Modérée (latence API) |

---

## 5. Recommandations pour l'Industrie

1.  **Human-in-the-loop :** Toujours proposer un mode `--dry-run` ou une sortie Markdown pour revue avant application.
2.  **Gestion des versions :** Intégrer un commentaire `<remarks>` indiquant que la doc a été générée par IA pour faciliter les audits.
3.  **Rate Limiting :** Utiliser des sémaphores pour gérer les limites de l'API LLM sans faire échouer le processus.
4.  **Standardisation :** Suivre les directives de Microsoft sur les balises XML (`<param>`, `<returns>`, `<exception>`).

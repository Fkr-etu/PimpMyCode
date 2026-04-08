# CSharpDocumentor

An AI-powered console tool that automatically generates XML documentation comments for undocumented C# code, using **Roslyn** for code analysis and the **Claude API** for documentation generation.

---

## Core Design Decisions

### 1. Granularity : one API call per class

The fundamental unit of analysis is the **class**, not the individual member.

Sending methods in isolation produces generic, low-quality documentation because the model lacks the context needed to understand what the code actually does. By sending the entire class, Claude can infer:

- The overall responsibility of the class
- The relationships between methods and fields
- The business domain from naming conventions and data structures

This also reduces API costs significantly — 20 methods in a class become 1 call instead of 20.

```
❌ Per-method  →  cheap context, generic docs, 20x more API calls
✅ Per-class   →  rich context, accurate docs, 1 API call
```

### 2. No conversational memory between calls

Each class is documented in a single, self-contained API call. There is no multi-turn conversation carried across classes.

Instead, relevant external context is **injected into each prompt** when needed:

- Implemented interface signatures
- Base class signature
- Project name and root namespace (in the system prompt)

This approach is more predictable, cheaper, and easier to debug than managing a growing conversation history.

### 3. Method bodies are truncated, not omitted

The body of a method provides useful context for understanding its intent. However, sending hundreds of lines of implementation logic adds noise and token cost without improving documentation quality.

**Rule:** method bodies longer than ~50 lines are replaced with a `// ... [body truncated, N lines]` comment. The signature and short bodies are always sent in full.

### 4. Claude returns structured JSON, not rewritten C# code

Claude is asked to return a JSON object mapping member names to their XML doc strings. **Roslyn handles all code rewriting.**

Asking Claude to return modified C# files would risk:
- Subtle reformatting that corrupts the AST
- Hallucinated changes to logic
- A complex and fragile diff/patch system

By keeping the roles separate — Claude generates docs, Roslyn injects them — both tasks stay simple and reliable.

```json
{
  "members": [
    {
      "memberName": "ApplyDiscounts",
      "xmlDoc": "/// <summary>\n/// Applies all eligible discounts to the order based on customer tier.\n/// </summary>\n/// <param name=\"order\">The order to process.</param>\n/// <param name=\"tier\">The customer's pricing tier.</param>\n/// <returns>The total discount amount applied.</returns>"
    }
  ]
}
```

---

## LLM Configuration Recommendations

To ensure high-quality, consistent documentation, the following settings are recommended:

### 1. System Prompt
The system prompt should establish the LLM's persona and strict output constraints.
**Example:**
> "You are an expert .NET software engineer specializing in clean code and documentation. Your task is to generate professional XML documentation comments for C# members.
> - Use standard tags: <summary>, <param>, <returns>, <exception>.
> - Keep descriptions concise but informative.
> - For methods, describe the *intent* rather than the implementation.
> - **Constraint:** Return ONLY a valid JSON object matching the requested schema. No conversational text, no markdown code blocks."

### 2. Temperature
- **Recommended: 0.2**
- **Why:** Documentation requires high consistency and factual accuracy. A low temperature minimizes "hallucinations" and ensures that the LLM follows the requested JSON format strictly.

### 3. Token Limits
- **Input Tokens:** 4,000 to 8,000 (depending on class size). Truncating method bodies helps stay within these limits.
- **Output Tokens:** 1,000 to 2,000 (sufficient for JSON mapping of documentation strings).

### 4. Model Selection
- **Claude 3.5 Sonnet:** Best for complex reasoning and following strict JSON schemas.
- **GPT-4o:** Excellent alternative with high coding proficiency.
- **Llama 3 (70B):** Recommended for local deployments if hardware allows.

---

## Architecture

```
CSharpDocumentor/
├── Program.cs                   Entry point, CLI argument parsing, orchestration loop
│
├── Analysis/
│   ├── ProjectScanner.cs        Discovers .cs files, builds the project summary
│   └── ClassContextBuilder.cs  Core: extracts a class + its resolved dependencies
│                                (interfaces, base class), truncates long bodies,
│                                and builds the final prompt payload
│
├── Generation/
│   └── DocumentationGenerator.cs  Calls the Claude API, parses the JSON response
│
├── Injection/
│   └── DocumentationInjector.cs   CSharpSyntaxRewriter that inserts XML trivia
│                                   into the AST at the correct position
│
└── IO/
    └── FileWriter.cs            Creates .bak backups, writes the modified source file
```

---

## Prompt Structure

Every API call is structured as follows:

```
SYSTEM (constant across all calls)
├── Project name and description
├── Root namespace
└── Instruction: return only valid JSON, no markdown fences

USER (per class)
├── Interfaces implemented (signatures only)
├── Base class (signature only, if any)
├── Full class source (method bodies truncated if > 50 lines)
└── Explicit list of member names that need documentation
```

---

## Implementation Roadmap

### Phase 1 — Analysis only
Parse a single `.cs` file with Roslyn and print all members that are missing XML documentation. No API calls yet. Validate that the detection logic is correct.

### Phase 2 — Documentation preview
Send a single class to the Claude API and print the JSON response to the console, without modifying any files. Validate prompt quality and JSON parsing.

### Phase 3 — Single-file injection
Inject the generated docs into one test file using the `DocumentationInjector` Roslyn rewriter. Validate that the output compiles and the formatting is clean.

### Phase 4 — Safe full-project mode
Add `--dry-run` (print changes without writing) and `--write` (apply changes with `.bak` backups) modes. Add a `SemaphoreSlim` to rate-limit concurrent API calls.

### Phase 5 — MSBuild workspace integration
Use `MSBuildWorkspace` to load a `.csproj` file with full semantic resolution. This enables accurate cross-file type information (e.g. resolving interfaces defined in other files).

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- An Anthropic API key

### Setup

```bash
git clone https://github.com/your-org/CSharpDocumentor
cd CSharpDocumentor
dotnet restore
```

### Dependencies

```bash
dotnet add package Microsoft.CodeAnalysis.CSharp
dotnet add package Microsoft.CodeAnalysis.Workspaces.MSBuild
dotnet add package Microsoft.Build.Locator
dotnet add package System.Net.Http.Json
```

### Usage

```bash
# Preview what would be documented (no files modified)
dotnet run -- --path /path/to/your/project --dry-run

# Apply documentation to all undocumented members
ANTHROPIC_API_KEY=sk-ant-... dotnet run -- --path /path/to/your/project --write
```

---

## Key Constraints and Gotchas

| Concern | Mitigation |
|---|---|
| API rate limits | `SemaphoreSlim` to cap concurrent requests |
| Large classes (> 600 lines) | Split into logical regions, two sequential calls |
| Accidental code modification | Claude returns JSON only; Roslyn does all rewriting |
| File corruption | `.bak` backup created before every write |
| Encoding issues | Roslyn preserves original encoding; write with `File.WriteAllTextAsync` |
| Generic-looking docs | Always send the full class, never isolated members |

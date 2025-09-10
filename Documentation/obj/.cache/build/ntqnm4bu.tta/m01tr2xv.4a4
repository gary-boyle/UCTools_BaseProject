<!DOCTYPE html>
<html lang="csharp">
  <head>
    <meta charset="utf-8">
      <title>Class SaveService
 | UCTools Base Project Documentation </title>
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <meta name="title" content="Class SaveService
 | UCTools Base Project Documentation ">
      
      
      <link rel="icon" href="../../favicon.ico">
      <link rel="stylesheet" href="../../public/docfx.min.css">
      <link rel="stylesheet" href="../../public/main.css">
      <meta name="docfx:navrel" content="">
      <meta name="docfx:tocrel" content="toc.html">
      
      <meta name="docfx:rel" content="../../">
      
      
      
      <meta name="loc:inThisArticle" content="In This Article">
      <meta name="loc:searchResultsCount" content="">
      <meta name="loc:searchNoResults" content="">
      <meta name="loc:tocFilter" content="Enter here to filter...">
      <meta name="loc:nextArticle" content="">
      <meta name="loc:prevArticle" content="">
      <meta name="loc:themeLight" content="">
      <meta name="loc:themeDark" content="">
      <meta name="loc:themeAuto" content="">
      <meta name="loc:changeTheme" content="">
      <meta name="loc:copy" content="">
      <meta name="loc:downloadPdf" content="">

      <script type="module" src="./../../public/docfx.min.js"></script>

      <script>
        const theme = localStorage.getItem('theme') || 'auto'
        document.documentElement.setAttribute('data-bs-theme', theme === 'auto' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : theme)
      </script>

  </head>

  <body class="tex2jax_ignore" data-layout="" data-yaml-mime="">
    <header class="bg-body border-bottom">
      <nav id="autocollapse" class="navbar navbar-expand-md" role="navigation">
        <div class="container-xxl flex-nowrap">
          <a class="navbar-brand" href="../../index.html">
            <img id="logo" class="svg" src="../../logo.svg" alt="">
            
          </a>
          <button class="btn btn-lg d-md-none border-0" type="button" data-bs-toggle="collapse" data-bs-target="#navpanel" aria-controls="navpanel" aria-expanded="false" aria-label="Toggle navigation">
            <i class="bi bi-three-dots"></i>
          </button>
          <div class="collapse navbar-collapse" id="navpanel">
            <div id="navbar">
              <form class="search" role="search" id="search">
                <i class="bi bi-search"></i>
                <input class="form-control" id="search-query" type="search" disabled="" placeholder="Search" autocomplete="off" aria-label="Search">
              </form>
            </div>
          </div>
        </div>
      </nav>
    </header>

    <main class="container-xxl">
      <div class="toc-offcanvas">
        <div class="offcanvas-md offcanvas-start" tabindex="-1" id="tocOffcanvas" aria-labelledby="tocOffcanvasLabel">
          <div class="offcanvas-header">
            <h5 class="offcanvas-title" id="tocOffcanvasLabel">Table of Contents</h5>
            <button type="button" class="btn-close" data-bs-dismiss="offcanvas" data-bs-target="#tocOffcanvas" aria-label="Close"></button>
          </div>
          <div class="offcanvas-body">
            <nav class="toc" id="toc"></nav>
          </div>
        </div>
      </div>

      <div class="content">
        <div class="actionbar">
          <button class="btn btn-lg border-0 d-md-none" type="button" data-bs-toggle="offcanvas" data-bs-target="#tocOffcanvas" aria-controls="tocOffcanvas" aria-expanded="false" aria-label="Show table of contents">
            <i class="bi bi-list"></i>
          </button>

          <nav id="breadcrumb"></nav>
        </div>

        <article data-uid="GameFramework.Services.SaveService">
  
  
  <h1 id="GameFramework_Services_SaveService" data-uid="GameFramework.Services.SaveService" class="text-break">
    Class SaveService
    
  </h1>
  
  <div class="facts text-secondary">
    <dl><dt>Namespace</dt><dd><a class="xref" href="GameFramework.Services.html">GameFramework.Services</a></dd></dl>
    <dl><dt>Assembly</dt><dd>cs.temp.dll.dll</dd></dl>
  </div>
  
  <div class="markdown summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="7">Enhanced save service that handles both file operations and UI support
Provides clean separation between low-level file I/O and high-level UI operations
Uses timestamp-based naming for all save files with special autosave handling
Autosaves always overwrite existing autosave files to prevent save directory bloat
Works with GameDataService as the single source of session data
Integrates with TimeService for accurate playtime tracking</p>
</div>
  <div class="markdown conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public class SaveService : ISaveService</code></pre>
  </div>
  
  
  
  
  <dl class="typelist inheritance">
    <dt>Inheritance</dt>
    <dd>
      <div><span class="xref">System.Object</span></div>
      <div><span class="xref">SaveService</span></div>
    </dd>
  </dl>
  
  
  
  
  
  
  
  
  
  <h2 class="section" id="constructors">Constructors
  </h2>
  
  
  <a id="GameFramework_Services_SaveService__ctor_" data-uid="GameFramework.Services.SaveService.#ctor*"></a>
  
  <h3 id="GameFramework_Services_SaveService__ctor_IEventSystem_" data-uid="GameFramework.Services.SaveService.#ctor(IEventSystem)">
    SaveService(IEventSystem)
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public SaveService(IEventSystem eventSystem)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>eventSystem</code> <span class="xref">IEventSystem</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  
  <h2 class="section" id="properties">Properties
  </h2>
  
  
  <a id="GameFramework_Services_SaveService_IsInitialized_" data-uid="GameFramework.Services.SaveService.IsInitialized*"></a>
  
  <h3 id="GameFramework_Services_SaveService_IsInitialized" data-uid="GameFramework.Services.SaveService.IsInitialized">
    IsInitialized
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public bool IsInitialized { get; }</code></pre>
  </div>

  
  
  
  
  <h4 class="section">Property Value</h4>
  <dl class="parameters">
    <dt><span class="xref">System.Boolean</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  <h2 class="section" id="methods">Methods
  </h2>
  
  
  <a id="GameFramework_Services_SaveService_CanSaveGame_" data-uid="GameFramework.Services.SaveService.CanSaveGame*"></a>
  
  <h3 id="GameFramework_Services_SaveService_CanSaveGame" data-uid="GameFramework.Services.SaveService.CanSaveGame">
    CanSaveGame()
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Checks if the game can currently be saved based on session state</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public bool CanSaveGame()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">System.Boolean</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_DeleteSaveAsync_" data-uid="GameFramework.Services.SaveService.DeleteSaveAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_DeleteSaveAsync_System_String_" data-uid="GameFramework.Services.SaveService.DeleteSaveAsync(System.String)">
    DeleteSaveAsync(String)
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;bool&gt; DeleteSaveAsync(string saveName)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>saveName</code> <span class="xref">System.String</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.Boolean</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_DeleteSaveFileByInfoAsync_" data-uid="GameFramework.Services.SaveService.DeleteSaveFileByInfoAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_DeleteSaveFileByInfoAsync_SaveFileInfo_" data-uid="GameFramework.Services.SaveService.DeleteSaveFileByInfoAsync(SaveFileInfo)">
    DeleteSaveFileByInfoAsync(SaveFileInfo)
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Deletes a save file using SaveFileInfo</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;bool&gt; DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>saveFileInfo</code> <span class="xref">SaveFileInfo</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.Boolean</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_GetMostRecentSaveName_" data-uid="GameFramework.Services.SaveService.GetMostRecentSaveName*"></a>
  
  <h3 id="GameFramework_Services_SaveService_GetMostRecentSaveName" data-uid="GameFramework.Services.SaveService.GetMostRecentSaveName">
    GetMostRecentSaveName()
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public string GetMostRecentSaveName()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">System.String</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_GetSaveFileInfoAsync_" data-uid="GameFramework.Services.SaveService.GetSaveFileInfoAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_GetSaveFileInfoAsync_System_String_" data-uid="GameFramework.Services.SaveService.GetSaveFileInfoAsync(System.String)">
    GetSaveFileInfoAsync(String)
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Gets formatted save file information for a specific save file</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;SaveFileInfo&gt; GetSaveFileInfoAsync(string saveName)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>saveName</code> <span class="xref">System.String</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">SaveFileInfo</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_GetSaveFileInfosAsync_" data-uid="GameFramework.Services.SaveService.GetSaveFileInfosAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_GetSaveFileInfosAsync" data-uid="GameFramework.Services.SaveService.GetSaveFileInfosAsync">
    GetSaveFileInfosAsync()
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="3">Gets formatted save file information for UI display, sorted by most recent first
Uses TimeService integration for accurate playtime display</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;SaveFileInfo[]&gt; GetSaveFileInfosAsync()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">SaveFileInfo</span>[]&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_GetSaveFilesAsync_" data-uid="GameFramework.Services.SaveService.GetSaveFilesAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_GetSaveFilesAsync" data-uid="GameFramework.Services.SaveService.GetSaveFilesAsync">
    GetSaveFilesAsync()
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;string[]&gt; GetSaveFilesAsync()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.String</span>[]&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_HasAnySaves_" data-uid="GameFramework.Services.SaveService.HasAnySaves*"></a>
  
  <h3 id="GameFramework_Services_SaveService_HasAnySaves" data-uid="GameFramework.Services.SaveService.HasAnySaves">
    HasAnySaves()
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public bool HasAnySaves()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">System.Boolean</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_InitializeAsync_" data-uid="GameFramework.Services.SaveService.InitializeAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_InitializeAsync" data-uid="GameFramework.Services.SaveService.InitializeAsync">
    InitializeAsync()
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task InitializeAsync()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span></dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_LoadGameSessionAsync_" data-uid="GameFramework.Services.SaveService.LoadGameSessionAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_LoadGameSessionAsync_System_String_" data-uid="GameFramework.Services.SaveService.LoadGameSessionAsync(System.String)">
    LoadGameSessionAsync(String)
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;GameSession&gt; LoadGameSessionAsync(string saveName)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>saveName</code> <span class="xref">System.String</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">GameSession</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_LoadGameSessionByInfoAsync_" data-uid="GameFramework.Services.SaveService.LoadGameSessionByInfoAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_LoadGameSessionByInfoAsync_SaveFileInfo_" data-uid="GameFramework.Services.SaveService.LoadGameSessionByInfoAsync(SaveFileInfo)">
    LoadGameSessionByInfoAsync(SaveFileInfo)
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Loads a game session using SaveFileInfo</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;GameSession&gt; LoadGameSessionByInfoAsync(SaveFileInfo saveFileInfo)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>saveFileInfo</code> <span class="xref">SaveFileInfo</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">GameSession</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_PerformAutoSaveAsync_" data-uid="GameFramework.Services.SaveService.PerformAutoSaveAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_PerformAutoSaveAsync" data-uid="GameFramework.Services.SaveService.PerformAutoSaveAsync">
    PerformAutoSaveAsync()
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Performs an autosave, always overwriting the existing autosave file</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;(bool success, string saveName)&gt; PerformAutoSaveAsync()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.ValueTuple</span>&lt;<span class="xref">System.Boolean</span>, <span class="xref">System.String</span>&gt;&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_PerformRegularSaveAsync_" data-uid="GameFramework.Services.SaveService.PerformRegularSaveAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_PerformRegularSaveAsync" data-uid="GameFramework.Services.SaveService.PerformRegularSaveAsync">
    PerformRegularSaveAsync()
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Performs a regular save with automatic timestamp-based naming</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;(bool success, string saveName)&gt; PerformRegularSaveAsync()</code></pre>
  </div>

  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.ValueTuple</span>&lt;<span class="xref">System.Boolean</span>, <span class="xref">System.String</span>&gt;&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_SaveGameSessionAsync_" data-uid="GameFramework.Services.SaveService.SaveGameSessionAsync*"></a>
  
  <h3 id="GameFramework_Services_SaveService_SaveGameSessionAsync_GameSession_System_String_System_Boolean_" data-uid="GameFramework.Services.SaveService.SaveGameSessionAsync(GameSession,System.String,System.Boolean)">
    SaveGameSessionAsync(GameSession, String, Boolean)
    
  </h3>
  
  <div class="markdown level1 summary"><p sourcefile="api/GameFramework.Services.SaveService.yml" sourcestartlinenumber="2" sourceendlinenumber="2">Legacy method for backward compatibility - now delegates to business logic methods</p>
</div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public async Task&lt;bool&gt; SaveGameSessionAsync(GameSession session, string saveName = null, bool isAutoSave = false)</code></pre>
  </div>

  <h4 class="section">Parameters</h4>
  <dl class="parameters">
    <dt><code>session</code> <span class="xref">GameSession</span></dt>
    <dd></dd>
    <dt><code>saveName</code> <span class="xref">System.String</span></dt>
    <dd></dd>
    <dt><code>isAutoSave</code> <span class="xref">System.Boolean</span></dt>
    <dd></dd>
  </dl>
  
  <h4 class="section">Returns</h4>
  <dl class="parameters">
    <dt><span class="xref">Task</span>&lt;<span class="xref">System.Boolean</span>&gt;</dt>
    <dd></dd>
  </dl>
  
  
  
  
  
  
  
  
  
  
  
  <a id="GameFramework_Services_SaveService_Shutdown_" data-uid="GameFramework.Services.SaveService.Shutdown*"></a>
  
  <h3 id="GameFramework_Services_SaveService_Shutdown" data-uid="GameFramework.Services.SaveService.Shutdown">
    Shutdown()
    
  </h3>
  
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>

  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public void Shutdown()</code></pre>
  </div>

  
  
  
  
  
  
  
  
  
  
  
  
</article>

        <div class="contribution d-print-none">
          
        </div>

        <div class="next-article d-print-none border-top" id="nextArticle"></div>

      </div>

      <div class="affix">
        <nav id="affix"></nav>
      </div>
    </main>

    <div class="container-xxl search-results" id="search-results"></div>

    <footer class="border-top text-secondary">
      <div class="container-xxl">
        <div class="flex-fill">
          UCTools Base Project Documentation
        </div>
      </div>
    </footer>
  </body>
</html>

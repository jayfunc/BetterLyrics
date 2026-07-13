import os
import re

directory = r"d:\Workspace\BetterLyrics\src\BetterLyricsKMP\shared\src\commonMain\kotlin\com\jayfunc\betterlyrics\viewmodels"
skip_files = [
    "NowPlayingBarViewModel.kt",
    "SettingsPageViewModel.kt",
    "LyricsSearchControlViewModel.kt",
    "PlayQueueViewModel.kt",
    "SystemTrayViewModel.kt"
]

def clean_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Step 1: Wipe everything inside methods using brace matching.
    # We find "fun ", "private void ", etc.
    # Then we find the first '{', count braces until depth is 0, and replace that whole block.
    # Because there might be syntax errors like "partial void OnSelectedTimeRangeChanged(StatsRange value)"
    
    # We'll just define a list of keywords that signify a method declaration in C#/Kotlin
    method_regex = re.compile(r"^\s*(?:override\s+|suspend\s+|private\s+|public\s+|partial\s+)?(?:async\s+)?(?:fun|void|Task(?:<.*?>)?|int|string|bool)\s+\w+\s*\(.*?\)", re.MULTILINE)
    
    def replace_methods(text):
        out = []
        pos = 0
        while True:
            match = method_regex.search(text, pos)
            if not match:
                out.append(text[pos:])
                break
                
            # Keep everything up to the method signature
            out.append(text[pos:match.start()])
            
            # The method signature
            sig = match.group(0).strip()
            # Convert C# sig to Kotlin 'fun' if needed
            sig = re.sub(r"^(?:override\s+|suspend\s+|private\s+|public\s+|partial\s+)?(?:async\s+)?(?:void|Task(?:<.*?>)?|int|string|bool)\s+", "fun ", sig)
            if not sig.startswith("fun "):
                sig = "fun " + sig
            
            # Find the opening brace '{'
            brace_start = text.find('{', match.end())
            if brace_start == -1:
                # No brace found? Just skip
                out.append("    " + sig + " { TODO() }\n")
                pos = match.end()
                continue
                
            # Track braces
            depth = 0
            brace_end = -1
            for i in range(brace_start, len(text)):
                if text[i] == '{':
                    depth += 1
                elif text[i] == '}':
                    depth -= 1
                    if depth == 0:
                        brace_end = i
                        break
            
            if brace_end != -1:
                out.append("    " + sig + " {\n        TODO(\"Port logic in Phase 4\")\n    }\n")
                pos = brace_end + 1
            else:
                out.append("    " + sig + " { TODO() }\n")
                pos = match.end()
                
        return "".join(out)

    content = replace_methods(content)
    
    # Step 2: Clean up any lingering C# stuff in properties
    lines = content.split('\n')
    new_lines = []
    for line in lines:
        cl = line
        cl = re.sub(r'\[ObservableProperty\]\s*(?:public\s+partial\s+)?([\w\?<>]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}', r'var \2: \1 by mutableStateOf(null /* TODO */)', cl)
        cl = cl.replace('[ObservableProperty]', '')
        cl = cl.replace('[NotifyPropertyChangedRecipients]', '')
        cl = re.sub(r'public\s+([\w\?<>]+)\s+(\w+)\s*\{\s*get;\s*(?:set;)?\s*\}', r'var \2: \1 = TODO()', cl)
        cl = re.sub(r'=\s*new\s*ObservableCollection<.*?>\(\)', '= mutableStateListOf()', cl)
        cl = re.sub(r'=\s*new\(\);?', '= null /* TODO */', cl)
        cl = cl.replace('new ', '')
        
        # Strip some lingering stuff
        if re.match(r'^\s*(case|switch|break|continue|default)', cl):
            continue
            
        if cl.strip() == "" and (len(new_lines) > 0 and new_lines[-1].strip() == ""):
            continue
            
        new_lines.append(cl)

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(new_lines))

for filename in os.listdir(directory):
    if filename.endswith(".kt") and filename not in skip_files:
        clean_file(os.path.join(directory, filename))

print("Python skeletonization complete.")

#!/usr/bin/env python3
"""Dependency-free structural checks. This is NOT a C# compiler or a Unity runtime test."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []
checks = 0

def check(condition: bool, description: str) -> None:
    global checks
    checks += 1
    if not condition:
        errors.append(description)

def tokens(source: str, filename: str) -> list[tuple[str, int]]:
    """Discard comments and string/char literals; preserve source line numbers."""
    result = []
    i, line, length = 0, 1, len(source)
    while i < length:
        c = source[i]
        if c.isspace():
            line += c == '\n'; i += 1; continue
        if source.startswith('//', i):
            end = source.find('\n', i + 2); i = length if end < 0 else end; continue
        if source.startswith('/*', i):
            end = source.find('*/', i + 2)
            if end < 0:
                errors.append(f'{filename}:{line}: unterminated comment'); break
            line += source[i:end+2].count('\n'); i = end + 2; continue
        verbatim = source.startswith('@"', i) or source.startswith('$@"', i) or source.startswith('@$"', i)
        start = i
        if verbatim:
            i += 3 if source[i:i+3] in ('$@"', '@$"') else 2
            while i < length:
                if source.startswith('""', i): i += 2; continue
                if source[i] == '"': i += 1; break
                i += 1
            else: errors.append(f'{filename}:{line}: unterminated verbatim string')
            line += source[start:i].count('\n'); result.append(('LITERAL', line)); continue
        if c in ('"', "'") or source.startswith('$"', i):
            if c == '$': i += 1; c = '"'
            i += 1
            while i < length:
                if source[i] == '\\': i += 2; continue
                if source[i] == c: i += 1; break
                i += 1
            else: errors.append(f'{filename}:{line}: unterminated literal')
            line += source[start:i].count('\n'); result.append(('LITERAL', line)); continue
        m = re.match(r'[A-Za-z_][A-Za-z0-9_]*|\d+(?:\.\d+)?', source[i:])
        if m:
            value = m.group(); result.append((value, line)); i += len(value)
        else: result.append((c, line)); i += 1
    return result

def run() -> dict:
    required = ['Assets/RagnaRock/Scenes/RagnaRock.unity', 'Packages/manifest.json',
                'ProjectSettings/ProjectVersion.txt', 'ProjectSettings/EditorBuildSettings.asset',
                'ProjectSettings/InputManager.asset', 'ProjectSettings/ProjectSettings.asset',
                'Assets/RagnaRock/Resources/StageLit.shader', 'Assets/RagnaRock/Resources/SonicFX.shader',
                'Tests/CoreHarness/CoreHarness.csproj', 'Tools/Publish-GitHub.ps1', 'Tools/Run-UnityChecks.ps1']
    for name in required: check((ROOT/name).is_file(), f'Missing required file: {name}')
    for path in list(ROOT.rglob('*.json')) + list(ROOT.rglob('*.asmdef')):
        if any(part in ('Library', 'obj', 'bin', '.git') for part in path.parts): continue
        try: json.loads(path.read_text(encoding='utf-8')); check(True, '')
        except (ValueError, UnicodeError) as ex: check(False, f'Invalid JSON {path.relative_to(ROOT)}: {ex}')
    campaign = json.loads((ROOT/'Assets/RagnaRock/Resources/Campaign.json').read_text(encoding='utf-8'))
    chapters = campaign.get('chapters', [])
    check(campaign.get('schemaVersion') == 1, 'Campaign schema')
    check(campaign.get('wavesPerChapter') == 3 and len(chapters) == 18, 'Expected 18 acts / 54 waves')
    check(len({c['id'] for c in chapters}) == len(chapters), 'Unique chapter IDs')
    for chapter in chapters:
        check(50 <= chapter['bpm'] <= 300, f"Invalid BPM {chapter['id']}")
        check(0 <= chapter['enemyFamily'] <= 6, f"Invalid family {chapter['id']}")
        check(re.fullmatch(r'#[0-9a-fA-F]{6}', chapter['colorHex']) is not None, f"Invalid color {chapter['id']}")
        for key in ('title','style','period','environment','lore','tribute','bossName'):
            check(bool(chapter.get(key)), f"Empty chapter field {chapter['id']}/{key}")
    check(chapters[0]['id'] == 'roots' and chapters[-1]['id'] == 'brutal-death', 'Campaign endpoints')
    check(sum(bool(c.get('songReference')) for c in chapters) >= 2, 'Explicit song references')
    guids: dict[str, str] = {}
    assets = ROOT/'Assets'
    for path in assets.rglob('*'):
        if path.suffix == '.meta': continue
        meta = Path(str(path)+'.meta')
        check(meta.exists(), f'Missing meta: {path.relative_to(ROOT)}')
    for meta in assets.rglob('*.meta'):
        original = Path(str(meta)[:-5])
        check(original.exists(), f'Orphan meta: {meta.relative_to(ROOT)}')
        found = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.MULTILINE)
        check(found is not None, f'Malformed GUID: {meta.relative_to(ROOT)}')
        if found:
            guid = found.group(1)
            check(guid not in guids, f'Duplicate GUID {guid}')
            guids[guid] = str(original.relative_to(ROOT))
    for path in [ROOT/'Assets/RagnaRock/Scenes/RagnaRock.unity', ROOT/'ProjectSettings/EditorBuildSettings.asset']:
        for guid in re.findall(r'guid: ([0-9a-f]{32})', path.read_text()):
            check(guid in guids, f'Unresolved scene/build GUID in {path.relative_to(ROOT)}: {guid}')
    scene = (ROOT/required[0]).read_text()
    script_guid = re.search(r'm_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})', scene)
    check(script_guid is not None and guids.get(script_guid.group(1), '').endswith('/RagnaBootstrap.cs'), 'Scene must reference bootstrap script')
    sources = [p for p in ROOT.rglob('*.cs') if not any(k in ('Library','bin','obj','.git') for k in p.parts)]
    lines = 0
    for path in sources:
        source = path.read_text(encoding='utf-8'); lines += len(source.splitlines())
        name = str(path.relative_to(ROOT)); parsed = tokens(source, name); stack = []
        for token, line in parsed:
            if token in ('{','[','('): stack.append((token,line))
            elif token in ('}',']',')'):
                expected = {'}':'{',']':'[',')':'('}[token]
                if not stack or stack[-1][0] != expected:
                    errors.append(f'{name}:{line}: unbalanced {token}'); break
                stack.pop()
        check(not stack, f'{name}: unclosed delimiters {stack}')
        # C# CS0819: implicitly typed locals cannot contain multiple declarators.
        for i, (token, line) in enumerate(parsed[:-3]):
            if token != 'var' or parsed[i+2][0] != '=': continue
            depth = 0
            for item, _ in parsed[i+3:]:
                if item == ';' and depth == 0: break
                if item in ('(', '[', '{'): depth += 1
                elif item in (')', ']', '}'):
                    if depth == 0: break
                    depth -= 1
                elif item == ',' and depth == 0: errors.append(f'{name}:{line}: multiple var declarators'); break
        for cls in re.findall(r'\bclass\s+(\w+)\s*:\s*MonoBehaviour\b', source):
            check(cls == path.stem, f'MonoBehaviour filename mismatch {name}/{cls}')
    project = ElementTree.parse(ROOT/'Tests/CoreHarness/CoreHarness.csproj')
    for compile_item in project.findall('.//Compile'):
        check((ROOT/'Tests/CoreHarness'/compile_item.attrib['Include']).resolve().exists(), 'Harness compile target exists')
    defs = {json.loads(p.read_text())['name'] for p in assets.rglob('*.asmdef')}
    for path in assets.rglob('*.asmdef'):
        definition=json.loads(path.read_text())
        for ref in definition.get('references',[]):
            check(ref in defs or ref == 'Unity.ugui', f'Unresolved asmdef {path.name}: {ref}')
    for path in assets.rglob('*'):
        check(path.suffix.lower() not in ('.ttf','.otf','.woff','.woff2'), f'Font binary must not be included: {path}')
    settings=(ROOT/'ProjectSettings/ProjectSettings.asset').read_text()
    check('activeInputHandler: 0' in settings, 'Legacy Input configuration matches implementation')
    for axis in ('Horizontal','Vertical','Submit','Cancel'):
        check('m_Name: '+axis in (ROOT/'ProjectSettings/InputManager.asset').read_text(), f'Input axis {axis}')
    test_sources='\n'.join(p.read_text() for p in assets.rglob('*Tests.cs'))
    test_cases=len(re.findall(r'\[(?:Test|UnityTest)\]', test_sources))
    return {'status':'PASS' if not errors else 'FAIL', 'structural_checks':checks, 'csharp_files':len(sources),
            'csharp_lines':lines,'unity_test_cases_authored':test_cases,'acts':len(chapters),
            'waves':len(chapters)*campaign['wavesPerChapter'],'errors':errors,
            'not_executed':['C# compilation','Unity import','Unity EditMode/PlayMode','shader compilation',
                            'gameplay/render/audio inspection','Windows build','remote GitHub workflows']}

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report',type=Path)
    args=parser.parse_args()
    try: report=run()
    except Exception as ex:
        report={'status':'FAIL','errors':[f'{type(ex).__name__}: {ex}'],'not_executed':['Unity and C# compilation']}
    print(json.dumps(report,ensure_ascii=False,indent=2))
    if args.report:
        args.report.parent.mkdir(parents=True,exist_ok=True)
        args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    sys.exit(0 if report['status']=='PASS' else 1)

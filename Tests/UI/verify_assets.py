"""Read-only checks of authored Unity IDs/bindings. Does not run Unity/import YAML."""
import re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
base = root / 'Assets/ProjectsFiles'
checks = 0

def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)

def documents(path):
    text = (base / path).read_text(encoding='utf-8-sig')
    docs = re.findall(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
    check(len(docs) == len(set(id for _, id, _ in docs)), f'{path}: duplicate file IDs')
    result = {id: (int(kind), body) for kind, id, body in docs}
    for kind, id, body in docs:
        for reference in re.findall(r'\{fileID: (\d+)\}', body):
            check(reference == '0' or reference in result, f'{path}: {id} refers to absent local ID {reference}')
    return text, result

loading_text, loading = documents('Prefabes/UI/LoadingCanvas.prefab')
match_text, match = documents('Prefabes/UI/MatchCanvas.prefab')
player_text, player = documents('Prefabes/Player.prefab')
legacy_text, legacy = documents('Prefabes/UI/LegacyNetworkLobbyCanvas.prefab')
menu_text, menu = documents('Scenes/MainMenuScene.unity')
game_text, game = documents('Scenes/GameScene.unity')
sample_text, sample = documents('Scenes/SampleScene.unity')

def binding(docs, component, field, kind, expected_class=None):
    value = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', docs[str(component)][1], re.M)
    check(value is not None and value[1] in docs, f'{component}: missing {field}')
    target = docs[value[1]]
    check(target[0] == kind, f'{component}.{field}: wrong object type')
    if expected_class:
        check(expected_class in target[1], f'{component}.{field}: wrong component class')
    return value[1]

binding(loading, 900010003, 'overlay', 223)
binding(loading, 900010003, 'inputBlocker', 114, 'UnityEngine.UI.GraphicRaycaster')
binding(loading, 900010003, 'status', 114, 'TMPro.TextMeshProUGUI')
binding(loading, 900010003, 'returnButton', 114, 'UnityEngine.UI.Button')
check('m_Father: {fileID: 0}' in loading['6694714962147940856'][1], 'Loading controller must live on its own root')
check('m_RenderMode: 0' in loading['2130930285395135702'][1], 'Loading uses screen overlay')
check('m_Enabled: 0' in loading['2130930285395135702'][1], 'Loading starts hidden without deactivating root')
check('m_Enabled: 0' in loading['6908285525652971972'][1], 'Hidden loading starts with no input blocker')
check('cf0f42b334bf1124d843ba35da4fd48e' in loading_text, 'Existing LoadingScreen image preserved')
check('m_AspectMode: 4' in loading['2330158595942626099'][1], 'Artwork uses EnvelopeParent')
check('m_AnchorMin: {x: 0.5, y: 0}' in loading['2065278308763012384'][1], 'Status remains Bottom Center')

binding(match, 900011003, 'matchCanvas', 223)
binding(match, 900011003, 'uiRaycaster', 114, 'UnityEngine.UI.GraphicRaycaster')
for field in ('timer', 'lifeStatus', 'resultTitle', 'firstTeam', 'secondTeam'):
    binding(match, 900011003, field, 114, 'TMPro.TextMeshProUGUI')
binding(match, 900011003, 'resultsPanel', 1)
binding(match, 900011003, 'back', 114, 'UnityEngine.UI.Button')
binding(match, 900011003, 'sceneEventSystem', 114, 'UnityEngine.EventSystems.EventSystem')
check('m_IsActive: 0' in match['5908618560838622175'][1], 'Results is not shown at match preparation')
check('m_Father: {fileID: 7949815740574562092}' in match['900013001'][1], 'Match EventSystem lifetime follows canvas')

binding(player, 900012003, 'playerCanvas', 223)
check('m_GameObject: {fileID: 6930759457325569799}' in player['900012003'][1], 'UI controller stays on player root')
check('m_GameObject: {fileID: 3428975614587819729}' in player['5328343828189518156'][1], 'Only PlayerCanvas is targeted')

for field in ('hostButton', 'clientButton'):
    binding(legacy, 900020003, field, 114, 'UnityEngine.UI.Button')
binding(legacy, 900020003, 'connectionStatus', 114, 'TMPro.TextMeshProUGUI')

def instances(text, guid):
    return len(re.findall(r'm_SourcePrefab: \{fileID: 100100000, guid: ' + guid, text))

check(instances(menu_text, '05467465f2d35174f800ab3c344454a8') == 1, 'Exactly one LoadingCanvas instance in menu')
check(instances(game_text, 'b1e8d286bd768954498c95f0609c0e7d') == 1, 'Exactly one MatchCanvas instance in game')
check(instances(sample_text, 'd87476b7b8a14cc8b7879ab934c095f2') == 1, 'Exactly one authored legacy UI in SampleScene')
check('900001003' not in menu, 'Old loading generator removed from scene')
check('190002002' not in game, 'Old match generator removed from scene')
check('operationStatus\n      value: \n      objectReference: {fileID: 728627839}' in menu_text, 'Menu notice references an authored text')

for path in (base / 'Scripts').rglob('*.cs'):
    text = path.read_text(encoding='utf-8-sig')
    check(not re.search(r'\bOnGUI\s*\(|\bGUILayout\.|new GameObject\([^;]*(?:typeof\((?:Canvas|Button|Image|TextMeshProUGUI|EventSystem)\))', text, re.S),
          f'{path}: runtime UI creation found')

print(f'PASS: {checks} authored UI asset/reference checks (not a Unity import/render test).')

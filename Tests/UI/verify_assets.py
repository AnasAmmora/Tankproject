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
row_text, row = documents('Prefabes/UI/ResultRow.prefab')
mate_text, mate = documents('Prefabes/UI/TeammateRow.prefab')
feed_text, feed = documents('Prefabes/UI/KillFeedRow.prefab')
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
for field in ('timer', 'lifeStatus', 'resultTitle'):
    binding(match, 900011003, field, 114, 'TMPro.TextMeshProUGUI')
for field in ('blueRows', 'redRows'):
    binding(match, 900011003, field, 224)
check('rowPrefab: {fileID: 901000004, guid: 15328f8ef6aa456fb6227ec76ff26693, type: 3}' in match_text,
      'MatchHud uses the authored ResultRow component prefab')
check('BlueResults' not in match_text and 'RedResults' not in match_text and
      'firstTeam:' not in match_text and 'secondTeam:' not in match_text, 'Old text elements/references removed')
for field in ('playerName', 'kills', 'deaths'):
    binding(row, 901000004, field, 114, 'TMPro.TextMeshProUGUI')
for field in ('evenBackground', 'oddBackground', 'localMarker'):
    binding(row, 901000004, field, 1)
check('m_HorizontalAlignment: 1' in row['901004003'][1], 'Player names align left')
check(all('m_HorizontalAlignment: 2' in row[id][1] for id in ('901005003','901006003')), 'Numbers align center')
check('HorizontalLayoutGroup' in row['901000002'][1], 'Rows use an authored HorizontalLayoutGroup')
for b in (902001000,902002000):
    check('VerticalLayoutGroup' in match[str(b+4)][1], 'Team table uses vertical layout')
    check('HorizontalLayoutGroup' in match[str(b+204)][1], 'Column header uses horizontal layout')
    check('VerticalLayoutGroup' in match[str(b+302)][1], 'Repeated rows use vertical layout')
    for cell_offset,row_cell,weight in ((210,'901004004',3),(220,'901005004',1),(230,'901006004',1)):
        check(f'm_FlexibleWidth: {weight}' in match[str(b+cell_offset+4)][1] and
              f'm_FlexibleWidth: {weight}' in row[row_cell][1], 'Header and row share the same column proportions')
binding(match, 900011003, 'resultsPanel', 1)
binding(match, 900011003, 'back', 114, 'UnityEngine.UI.Button')
binding(match, 900011003, 'sceneEventSystem', 114, 'UnityEngine.EventSystems.EventSystem')
binding(match, 900011003, 'gameplayHud', 114, 'GameplayHud')
check('m_IsActive: 0' in match['5908618560838622175'][1], 'Results is not shown at match preparation')
check('m_Father: {fileID: 7949815740574562092}' in match['900013001'][1], 'Match EventSystem lifetime follows canvas')

binding(player, 900012003, 'playerCanvas', 223)
player_ui = next(id for id, (kind, body) in player.items() if kind == 114 and 'guid: 29aa6df3da0847ae881e5d281c2ac26a,' in body)
check(f'component: {{fileID: {player_ui}}}' in player['6930759457325569799'][1],
      'PlayerUI must be attached to the actual network player root, not just exist as a document')
check('m_Enabled: 1' in player[player_ui][1], 'PlayerUI must be enabled')
check('m_LocalScale: {x: 1, y: 1, z: 1}' in player['5648818007334600302'][1],
      'PlayerCanvas must not have zero scale')

def hierarchy(docs, root_transform):
    visited = set()
    def walk(id):
        check(id not in visited, f'Hierarchy cycle/duplicate child: {id}')
        visited.add(id)
        body = docs[id][1]
        owner = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)[1]
        for component in re.findall(r'component: \{fileID: (\d+)\}', docs[owner][1]):
            check(f'm_GameObject: {{fileID: {owner}}}' in docs[component][1], f'{owner}: component owner mismatch')
        children = re.search(r'  m_Children:(.*?)(?=  m_Father:)', body, re.S)[1]
        for child in re.findall(r'fileID: (\d+)', children):
            check(f'm_Father: {{fileID: {id}}}' in docs[child][1], f'{child}: inconsistent hierarchy parent')
            walk(child)
    walk(str(root_transform))
    return visited

reachable = hierarchy(player, 1871954718826377230)
for widget in (910001001, 910020001, 910030001, 910040001, 910003001, 910034001):
    check(str(widget) in reachable, f'Player UI widget {widget} must be reachable from network player root')
hierarchy(match, 7949815740574562092)
for field in ('health','shooter','state'):
    binding(player,player_ui,field,114)
for field in ('combat','abilitySlot','weaponReady','hitMarker','killMarker'):
    binding(player,player_ui,field,1)
for field in ('reload','healthFill'):
    binding(player,player_ui,field,114,'UnityEngine.UI.Image')
for field in ('healthText','killText'):
    binding(player,player_ui,field,114,'TMPro.TextMeshProUGUI')
for field in ('lowHealth','confirmation'):
    binding(player,player_ui,field,225)
check('m_FillMethod: 4' in player['910003003'][1], 'Reload ring is authored Radial360')
check('m_FillMethod: 0' in player['910034003'][1], 'Health bar is authored horizontal fill')
check('m_Interactable: 0' in player['910040004'][1], 'Special ability remains an unavailable placeholder')
check('m_IsActive: 0' in player['910043000'][1], 'No fake special-ability cooldown is displayed')
check('m_Children: []' in player['6720335323447062107'][1], 'Original crosshair preserved')
for b in (910010000,910011000,910012000):
    binding(player,b+3,'bearing',224)
    binding(player,b+3,'fade',225)
for field in ('gameplayRoot','teammatePanel'):
    binding(match,920040003,field,1)
for field in ('timer','phaseLabel','lifeStatus'):
    binding(match,920040003,field,114,'TMPro.TextMeshProUGUI')
for field in ('timerAccent','baseNotice','overtimeNotice'):
    binding(match,920040003,field,225)
for field in ('teammateRows','feedRows'):
    binding(match,920040003,field,224)
check('teammatePrefab: {fileID: 930000005, guid:' in match_text and 'feedPrefab: {fileID: 940000007, guid:' in match_text,
      'General HUD instantiates only authored row prefabs')
for field in ('playerName','healthText','lifeText'):
    binding(mate,930000005,field,114,'TMPro.TextMeshProUGUI')
binding(mate,930000005,'healthFill',114,'UnityEngine.UI.Image')
for field in ('killer','action','victim'):
    binding(feed,940000007,field,114,'TMPro.TextMeshProUGUI')
binding(feed,940000007,'fade',225)
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

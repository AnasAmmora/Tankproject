"""Read-only saved-asset checks; does not launch Unity or prove rendering."""
import re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
assets = root / 'Assets/ProjectsFiles'
text = (assets / 'Prefabes/Player.prefab').read_text(encoding='utf-8-sig')
docs = re.findall(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
assert len(docs) == len({entry[1] for entry in docs}), 'Duplicate prefab file IDs'
by_id = {id: (int(kind), body) for kind, id, body in docs}
for kind, id, body in docs:
    assert body.count('\nMonoBehaviour:') == 0, f'Duplicate component body in {id}'
    for ref in re.findall(r'\{fileID: (\d+)\}', body):
        assert ref == '0' or ref in by_id, f'Missing local reference {ref} in {id}'

guid = re.search(r'^guid: (\w+)', (assets / 'Scripts/Player/TankTrackVisuals.cs.meta').read_text(), re.M)[1]
components = [(id, body) for kind, id, body in docs if f'guid: {guid},' in body]
assert len(components) == 1, 'Expected exactly one TankTrackVisuals'
component_id, component = components[0]
owner_id = re.search(r'm_GameObject: \{fileID: (\d+)\}', component)[1]
assert 'm_Name: Player\n' in by_id[owner_id][1]
assert f'component: {{fileID: {component_id}}}' in by_id[owner_id][1]
movement = next(body for _, _, body in docs if 'Assembly-CSharp::PlayerMovement\n' in body)
assert f'trackVisuals: {{fileID: {component_id}}}' in movement

wheels = re.findall(r'fileID: (\d+)', component.split('  wheels:\n')[1].split('  trackRenderer:')[0])
assert len(wheels) == len(set(wheels)) == 6
for index, wheel in enumerate(wheels, 1):
    kind, transform = by_id[wheel]
    assert kind == 4
    game_id = re.search(r'm_GameObject: \{fileID: (\d+)\}', transform)[1]
    assert f'm_Name: Wheel{index}\n' in by_id[game_id][1]
    component_ids = re.findall(r'component: \{fileID: (\d+)\}', by_id[game_id][1])
    assert sorted(by_id[id][0] for id in component_ids) == [4, 23, 33], 'Wheel must remain visual only'
    assert 'm_Father: {fileID: 776674280186094575}' in transform

renderer_id = re.search(r'trackRenderer: \{fileID: (\d+)\}', component)[1]
kind, renderer = by_id[renderer_id]
assert kind == 23
track_id = re.search(r'm_GameObject: \{fileID: (\d+)\}', renderer)[1]
assert 'm_Name: Track\n' in by_id[track_id][1]
assert 'materialSlot: 0\n' in component and 'textureProperty: _BaseMap\n' in component
material_refs = re.findall(r'guid: (\w+)', renderer.split('  m_Materials:\n')[1].split('  m_StaticBatchInfo:')[0])
assert material_refs == ['5c8fe8fa9b039e3458aae1f478602ed6']
material = (assets / 'Models/TREADMILE.mat').read_text()
base_map = material.split('    - _BaseMap:\n')[1].split('    - ')[0]
assert 'm_Scale: {x: 1, y: 1}' in base_map and 'm_Offset: {x: 0, y: 0.33}' in base_map
assert 'guid: 933532a4fcc9baf4fa0491de14d08ed7' in material, 'Expected URP Lit'
texture_meta = (assets / 'Models/tread Color.png.meta').read_text()
assert 'wrapV: 0' in texture_meta, 'Track texture Y must repeat'
assert '60f3a10b3a8c28a42bc1b9c4d2e40c3d' in (assets / 'Prefabes/Managers/NetworkManager.prefab').read_text()
print('PASS: saved Player prefab, six wheel bindings, track slot/shader/offset, unique component and local references.')

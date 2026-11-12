from pathlib import Path
src=(Path(__file__).resolve().parents[1]/"src/EDLController.cs").read_text()
assert '[Authorize]' in src
assert 'User.Identity?.IsAuthenticated' in src
assert 'GetItemById<BaseItem>(itemId, userId)' in src
assert 'GetEDL([FromRoute] Guid itemId' in src
assert 'EDLSidecarResolver.Resolve(item.Path)' in src
assert 'filePath' not in src
assert 'FileMode.Open' in src and 'FileAccess.Read' in src
print('Source security invariants OK (not a substitute for compiling).')

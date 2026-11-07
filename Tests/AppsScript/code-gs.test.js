// Run: node Tests/AppsScript/code-gs.test.js  (mocks DriveApp, checks folder routing of AppsScript/Code.gs)
const fs = require('fs'), vm = require('vm'), path = require('path'), assert = require('assert');
function folder(name) {
  const subs = {}, files = [];
  return { name, subs, files, getName: () => name,
    getFoldersByName: n => { const f = subs[n]; let d = !f; return { hasNext: () => !d, next: () => { d = true; return f; } }; },
    createFolder: n => (subs[n] = folder(n)),
    getFilesByName: () => ({ hasNext: () => false }),
    createFile: b => { files.push(b); return { getId: () => 'id' }; } };
}
const root = folder('DATA');
const ctx = {
  DriveApp: { getFolderById: () => root },
  Utilities: { base64Decode: s => Buffer.from(s, 'base64'), newBlob: (d, mime, name) => ({ mime, name }) },
  LockService: { getScriptLock: () => ({ waitLock() {}, releaseLock() {} }) },
  ContentService: { createTextOutput: t => ({ setMimeType: () => JSON.parse(t) }), MimeType: { JSON: 'json' } },
};
vm.createContext(ctx);
vm.runInContext(fs.readFileSync(path.join(__dirname, '../../AppsScript/Code.gs'), 'utf8'), ctx);
const post = p => ctx.doPost({ parameter: p });

assert(post({ site: 'LITV', fileName: 'a.csv', fileData: 'YQ==' }).success);
assert.strictEqual(root.subs.LITV.subs['PC-Info'].files[0].mime, 'text/csv');               // SM-Tool
assert(post({ site: 'HOT', subFolder: 'SoftAudit', fileName: 'b.xlsx', fileData: 'YQ==' }).success);
assert.match(root.subs.HOTEL.subs.SoftAudit.files[0].mime, /spreadsheetml/);                // SoftAudit
assert(post({ fileName: 'old.csv', fileData: 'YQ==' }).success);
assert.strictEqual(root.files[0].name, 'old.csv');                                          // old tool, no site
assert(!post({ site: 'LDS', subFolder: '../x', fileName: 'c.csv', fileData: 'YQ==' }).success);
assert(!post({ site: 'LDS/..', fileName: 'c.csv', fileData: 'YQ==' }).success);
console.log('Code.gs: ALL PASS');

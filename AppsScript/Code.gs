// Web app chung cho SM-Tool va SoftAudit - nhan file tu 4 site, luu vao Drive > DATA.
//
// Cau truc thu muc:
//   DATA/<SITE>/PC-Info/   <- CSV thong tin may (SM-Tool, khong gui subFolder)
//   DATA/<SITE>/SoftAudit/ <- Excel SoftAudit (gui subFolder=SoftAudit)
//   DATA/                  <- tool cu khong gui "site" (giu nhu truoc)
//
// Cap nhat: dan code nay vao project cu, Save, roi Deploy > Manage deployments > Edit (but chi)
// > Version: New version > Deploy. KHONG tao deployment moi (URL /exec se doi).

var FOLDER_ID = "1pkcngtArpemfOQuhnJRg892dKQQdiixW"; // ID thu muc DATA (lay tu URL khi mo thu muc)

// Ten site tool gui len -> ten thu muc tren Drive
var SITE_FOLDERS = {
  "LITV":    "LITV",
  "HOT":     "HOTEL",
  "LDS":     "LDS",
  "Coralis": "CORALIS"
};

var DEFAULT_SUBFOLDER = "PC-Info"; // SM-Tool khong gui subFolder
var MIME_TYPES = {
  "csv":  "text/csv",
  "xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
};

function doPost(e) {
  try {
    var root = DriveApp.getFolderById(FOLDER_ID);

    var fileName = e.parameter.fileName;
    var fileDataBase64 = e.parameter.fileData;
    var site = (e.parameter.site || "").trim();
    var subFolder = (e.parameter.subFolder || DEFAULT_SUBFOLDER).trim();

    if (!fileName || !fileDataBase64) {
      return json({success: false, error: "Missing fileName or fileData"});
    }

    // Chi chap nhan ten gon (chu, so, - _), tranh tao thu muc rac
    if (site && !/^[A-Za-z0-9_-]{1,30}$/.test(site)) {
      return json({success: false, error: "Invalid site"});
    }
    if (!/^[A-Za-z0-9_-]{1,30}$/.test(subFolder)) {
      return json({success: false, error: "Invalid subFolder"});
    }

    var folder = root;
    if (site) {
      folder = getOrCreateSubFolder(root, SITE_FOLDERS[site] || site);
      folder = getOrCreateSubFolder(folder, subFolder);
    }

    var ext = fileName.split(".").pop().toLowerCase();
    var mimeType = MIME_TYPES[ext] || "application/octet-stream";
    var blob = Utilities.newBlob(Utilities.base64Decode(fileDataBase64), mimeType, fileName);

    // Neu file cung ten da ton tai thi xoa ban cu (ghi de bang ban moi) -> tool gui lai an toan
    var existing = folder.getFilesByName(fileName);
    while (existing.hasNext()) {
      existing.next().setTrashed(true);
    }

    var file = folder.createFile(blob);

    return json({success: true, fileId: file.getId(), folder: folder.getName()});
  } catch (err) {
    return json({success: false, error: err.toString()});
  }
}

function getOrCreateSubFolder(parent, name) {
  // Khoa de 2 may cung site gui cung luc khong tao trung 2 thu muc
  var lock = LockService.getScriptLock();
  lock.waitLock(20000);
  try {
    var it = parent.getFoldersByName(name);
    return it.hasNext() ? it.next() : parent.createFolder(name);
  } finally {
    lock.releaseLock();
  }
}

function json(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj))
    .setMimeType(ContentService.MimeType.JSON);
}

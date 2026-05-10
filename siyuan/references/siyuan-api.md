# SiYuan API Reference

Complete reference of SiYuan kernel API endpoints. Use `raw <endpoint> <json>` for any endpoint not in the command table.

## Notebook Operations

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/notebook/lsNotebooks` | — | List all notebooks |
| `/api/notebook/openNotebook` | `notebook` | Open/mount a notebook |
| `/api/notebook/closeNotebook` | `notebook` | Close/unmount a notebook |
| `/api/notebook/createNotebook` | `name` | Create new notebook |
| `/api/notebook/removeNotebook` | `notebook` | Delete notebook |
| `/api/notebook/renameNotebook` | `notebook` `name` | Rename notebook |
| `/api/notebook/getNotebookConf` | `notebook` | Get notebook config |
| `/api/notebook/setNotebookConf` | `notebook` `conf` | Set notebook config |
| `/api/notebook/setNotebookIcon` | `notebook` `icon` | Set notebook icon |
| `/api/notebook/changeSortNotebook` | `notebooks[]` | Reorder notebooks |
| `/api/notebook/getNotebookInfo` | `notebook` | Get notebook info |

## File Tree Operations

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/filetree/listDocTree` | `notebook` `path` | Get doc tree structure |
| `/api/filetree/listDocsByPath` | `notebook` `path` | List docs at path |
| `/api/filetree/getDoc` | `id` | Get doc content JSON |
| `/api/filetree/searchDocs` | `k` | Search doc titles |
| `/api/filetree/createDocWithMd` | `notebook` `path` `markdown` | Create doc from markdown |
| `/api/filetree/createDoc` | `notebook` `path` `title` `md` | Create doc with options |
| `/api/filetree/createDailyNote` | `notebook` | Create today's daily note |
| `/api/filetree/renameDocByID` | `id` `title` | Rename doc by ID |
| `/api/filetree/renameDoc` | `notebook` `path` `title` | Rename doc by path |
| `/api/filetree/removeDocByID` | `id` | Remove doc by ID |
| `/api/filetree/removeDoc` | `notebook` `path` | Remove doc by path |
| `/api/filetree/moveDocsByID` | `fromIDs[]` `toID` | Move docs by ID |
| `/api/filetree/moveDocs` | `fromPaths[]` `toPath` `toNotebook` | Move docs by path |
| `/api/filetree/duplicateDoc` | `id` | Duplicate a document |
| `/api/filetree/doc2Heading` | `srcID` `targetID` `after` | Convert doc to heading |
| `/api/filetree/heading2Doc` | `srcHeadingID` `targetNotebook` | Convert heading to doc |
| `/api/filetree/getHPathByID` | `id` | Get h-path by ID |
| `/api/filetree/getFullHPathByID` | `id` | Get full h-path with parents |
| `/api/filetree/getPathByID` | `id` | Get path by ID |
| `/api/filetree/getIDsByHPath` | `notebook` `path` | Get IDs by h-path |
| `/api/filetree/getDocCreateSavePath` | `notebook` | Get default save path |
| `/api/filetree/changeSort` | `notebook` `paths[]` | Change sort order |

## Block Operations

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/block/getBlockInfo` | `id` | Get block metadata |
| `/api/block/getBlockBreadcrumb` | `id` | Get block breadcrumb |
| `/api/block/getChildBlocks` | `id` | Get child blocks |
| `/api/block/getBlockKramdown` | `id` | Get kramdown format |
| `/api/block/getRefIDs` | `id` | Get block references |
| `/api/block/insertBlock` | `data` `dataType` `parentID` | Insert new block |
| `/api/block/updateBlock` | `id` `data` `dataType` | Update block content |
| `/api/block/moveBlock` | `id` `parentID?` `previousID?` | Move block |
| `/api/block/deleteBlock` | `id` | Delete a block |
| `/api/block/foldBlock` | `id` | Fold a block |
| `/api/block/unfoldBlock` | `id` | Unfold a block |
| `/api/block/getDocInfo` | `id` | Get document info |
| `/api/block/getRecentUpdatedBlocks` | — | Get recently updated blocks |
| `/api/block/checkBlockExist` | `id` | Check if block exists |
| `/api/block/getBlocksWordCount` | `ids[]` | Get word counts |
| `/api/block/prependBlock` | `data` `dataType` `parentID` | Prepend block to parent |
| `/api/block/appendBlock` | `data` `dataType` `parentID` | Append block to parent |

## Search

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/search/fullTextSearchBlock` | `query` | Full-text search |
| `/api/search/searchTag` | `k` | Search tags |
| `/api/search/searchTemplate` | `k` | Search templates |
| `/api/search/searchRefBlock` | `id` `k` | Search block references |
| `/api/search/searchEmbedBlock` | `embedBlockID` `stmt` | Search embed blocks |
| `/api/search/searchAsset` | `k` | Search asset files |
| `/api/search/findReplace` | `k` `r` | Find and replace |
| `/api/search/listInvalidBlockRefs` | — | List invalid references |

## Export

| Endpoint | Format arg | Description |
|----------|------------|-------------|
| `/api/export/exportMd` | `md` | Export as Markdown |
| `/api/export/exportMdContent` | `md` | Get markdown content |
| `/api/export/exportSY` | `sy` | Export as .sy bundle |
| `/api/export/exportPreviewHTML` | `html` | Preview as HTML |
| `/api/export/exportDocx` | `docx` | Export as DOCX |
| `/api/export/exportEPUB` | `epub` | Export as EPUB |
| `/api/export/exportODT` | `odt` | Export as ODT |
| `/api/export/exportRTF` | `rtf` | Export as RTF |
| `/api/export/exportTextile` | `textile` | Export as Textile |
| `/api/export/exportOrgMode` | `org` | Export as Org Mode |
| `/api/export/exportReStructuredText` | `asciidoc` | Export as RST |
| `/api/export/processPDF` | `pdf` | Process PDF |
| `/api/export/exportData` | — | Export all data |

## Attributes

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/attr/getBlockAttrs` | `id` | Get block attributes |
| `/api/attr/setBlockAttrs` | `id` `attrs` | Set block attributes |
| `/api/attr/batchGetBlockAttrs` | `ids[]` | Batch get attributes |
| `/api/attr/batchSetBlockAttrs` | `blockAttrs[]` | Batch set attributes |
| `/api/attr/getBookmarkLabels` | — | Get bookmark labels |

## History & Backlinks

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/history/getDocHistoryContent` | `notebook` `path` | Get doc history |
| `/api/ref/getBacklink` | `id` | Get backlinks |
| `/api/ref/getBacklink2` | `id` | Get backlinks v2 |
| `/api/ref/refreshBacklink` | `id` | Refresh backlinks |

## SQL

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/query/sql` | `stmt` | Execute SQL query |
| `/api/sqlite/flushTransaction` | — | Flush transaction queue |

## Templates

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/template/render` | `id` `path` | Render template |
| `/api/template/docSaveAsTemplate` | `path` | Save as template |

## Outline

| Endpoint | Args | Description |
|----------|------|-------------|
| `/api/outline/getDocOutline` | `id` | Get document outline/headings |

## Key Tables

- `blocks` — all blocks (id, type, content, path, hpath, parent_id, box)
- `all_blocks` — same as blocks, may include virtual blocks
- `recent_blocks` — recently updated blocks
- `bookmarks` — bookmark labels
- `tags` — tag index

## Common ID Patterns

- Notebook ID: `box` field in blocks table (e.g., `20210727000000-0`)
- Doc ID: timestamp format (e.g., `20260402222543-0ex8h5n`)
- Block ID: same timestamp format, used inside docs

## Response Format

All API responses follow:
```json
{ "code": 0, "msg": "", "data": { ... } }
```

`code: 0` means success. Non-zero `code` indicates error with a `msg` field.
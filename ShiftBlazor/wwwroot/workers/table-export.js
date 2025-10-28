importScripts("javascript-number-formatter.min.js")
// Docs: https://mottie.github.io/javascript-number-formatter/

async function fetchRows(url, headers) {
    const response = await fetch(url, { headers, method: "GET" })
    const data = await response.json()
    return data?.Value || []
}

function buildForeignColumnsMapper(columns, foreignColumns, origin) {
    const foreignTables = {}
    const fieldMapper = {}

    foreignColumns.forEach((col) => {

        // if the foreign is not in the columns ( hidden ) then skip it
        if (!columns.some((column) => column.key === col.propertyName)) return

        foreignTables[col.foreignEntityField] ??= {
            items: [],
            itemsMapper: {},
            filterValues: {},
            url: (`${col.url || origin}/${col.entitySet}`).replaceAll("//", "/").replace(":/", "://")
        }

        fieldMapper[col.propertyName] = {
            table: col.foreignEntityField,
            idKey: col.tEntityValueField,
            valueKey: col.tEntityTextField,
        }

    })

    return { foreignTables, fieldMapper }
}

function populateForeignFilterValues(rows, foreignTables, fieldMapper) {

    // if there is no foreign columns then skip populating
    if (!Object.keys(fieldMapper).length) return

    // push unique values into filterValues
    rows.forEach((row) => {
        Object.entries(fieldMapper).forEach(([k, { table, idKey, valueKey }]) => {
            const value = row[k]
            if (value && `${value}`.length) {
                if (foreignTables[table].filterValues[idKey]) {
                    if (!foreignTables[table].filterValues[idKey].includes(value)) foreignTables[table].filterValues[idKey].push(value)
                } else {
                    foreignTables[table].filterValues[idKey] = [value]
                }
            }
        })
    })
}

function createODataQuery(filters) {
    const filterClauses = Object.entries(filters).map(([key, values]) => {
        const quoted = values.map((v) => `'${v}'`).join(",");
        return `${key} in (${quoted})`;
    });

    const filterClause = filterClauses.join(" and ");
    return `?$filter=${encodeURIComponent(filterClause)}`;
}

function fetchForeignEntries(foreignTables, headers) {
    const fetchPromises = Object.entries(foreignTables).map(([tableName, v]) => {

        let processFetching = false

        Object.values(v.filterValues).forEach(filterValue => {
            if (!!filterValue.length) processFetching = true
        })

        if (!processFetching) return

        const url = v.url + createODataQuery(v.filterValues)

        // this number can be increased as modern browsers and server support large URLs
        if (url.length > 2000) {
            console.error(`For foregign column: ${tableName}, URL exceeds maximum allowed length of 2000 characters. Actual length: ${url.length}`)
            return
        }

        return fetch(url, { headers, method: "GET" })
            .then(async (response) => {
                if (!response.ok) {
                    throw new Error(`Failed to fetch ${url}: ${response.statusText}`)
                }

                let items;

                try {
                    items = (await response.json()).Value;
                } catch (err) {
                    items = [];
                }

                v.items = [...v.items, ...items]
            })
    })

    return Promise.all(fetchPromises)
}

function generateItemMapper(foreignTables) {
    Object.values(foreignTables).forEach((v) => {
        // create entry mapper as id : value, for example "u23" : 1 (the index)
        v.items.forEach((item, idx) => {
            if (item.ID) {
                v.itemsMapper[item.ID.toString()] = idx.toString()
            }
        })
    })
}

function capitalizeFirstLetter(str) {
    return str.charAt(0).toUpperCase() + str.slice(1)
}

function parseRawValue(value, col, localizedColumns, lang) {
    if (col.enumValues && typeof col.enumValues[value] === "string") {
        try {
            value = col.enumValues[value]
        } catch { }
    }
    

    if (localizedColumns.includes(col.title)) { // normal texts localized
        try {
            const localizedObject = JSON.parse(value)
            value = localizedObject[lang] ?? value
        } catch { }
    }

    try {
        if (col.format) {
            let tempValue = +value
            value = format(col.format, tempValue)
        }
    } catch { }

    if (value instanceof Date) {
        value = toISOLocal(value)
    } else if (typeof value === "boolean") {
        value = capitalizeFirstLetter(String(value))
    }

    // Escape special characters for CSV formatting
    if (
        typeof value === "string" &&
        (value.includes(",") || value.includes("\n") || value.includes('"'))
    ) {
        value = `"${value.replace(/"/g, '""')}"`
    }

    return value
}

function parseColumn(col, foreignKeys, fieldMapper, row, foreignTables) {
    let value = row[col.key]

    if (foreignKeys.includes(col.key)) { // process the foriegn values
        if (value) {
            const foreignField = fieldMapper[col.key]
            const foreign = foreignTables[foreignField.table]

            let table = {}

            if (foreign.itemsMapper[value]) {
                table = foreign.items[+foreign.itemsMapper[value]]
            } else {
                table = foreign.items.find(item => item[foreignField.idKey] == value)
            }

            if (table && table[foreignField?.valueKey]) value = table[foreignField.valueKey]

            if (!table && foreignField?.valueKey) value = ""
        }

    } else if (value && col.type.startsWith('DateTime')) { // process dateTime columns
        value = new Date(value);
    }

    return value;

}

function parseCustomColumn(row, col, localizedColumns, lang, fieldMapper, foreignTables) {
    var values = col.customColumn.args.map(arg => {
        if (arg.includes(".")) {
            const [tableTarget, fieldTarget] = arg.split(".")

            if (foreignTables[tableTarget]) {
                const [foreignColumnKey, foreignColumnData] = Object.entries(fieldMapper).find(([_, ref]) => ref.table === tableTarget)

                const foreign = foreignTables[tableTarget]

                let foreignTableRow = {}

                try {
                    if (foreign.itemsMapper[row[foreignColumnKey]]) {
                        foreignTableRow = foreign.items[+foreign.itemsMapper[row[foreignColumnKey]]]
                    } else {
                        foreignTableRow = foreign.items.find(item => item[foreignColumnData.idKey] == row[foreignColumnKey])
                    }

                    let tempValue = foreignTableRow[fieldTarget];

                    if (typeof tempValue === "string") {
                        try {
                            const localizedObject = JSON.parse(tempValue)
                            tempValue = localizedObject[lang]
                        } catch { }
                    }

                    return parseRawValue(tempValue, col, localizedColumns, lang)
                } catch { }


            }
        } else {
            if (row[arg]) {
                if (typeof row[arg] === "string") {
                    let tempValue = row[arg];

                    try {
                        const localizedObject = JSON.parse(tempValue)
                        tempValue = localizedObject[lang]
                    } catch { }

                    return parseRawValue(tempValue, col, localizedColumns, lang)

                } else {
                    return parseRawValue(row[value], col, localizedColumns, lang)
                }
            }
        }
    });

    return formatUnicorn(col.customColumn.format, values);
}

function generateCSVContent(rows, columns, foreignTables, fieldMapper, lang) {
    const csvRows = []
    const foreignKeys = Object.keys(fieldMapper)
    const visibleColumns = columns.filter(col => !col.hidden)
    const localizedColumns = visibleColumns.filter(col => col.type === "LocalizedText").map(col => col.title)

    // Header row
    csvRows.push(visibleColumns.map((c) => {
        // remove line breaks
        if (c.title.includes("\n") || c.title.includes("\r")) {
            c.title = c.title.replace(/(?:\r?\n|\r)+/g, " ")
        } 

        if (c.title.includes(",") || c.title.includes('"')) {
            c.title = `"${c.title.replace(/"/g, '""')}"`
        }
        return c.title;
    }).join(","))

    rows.forEach((row, rowIndex) => {
        const csvRowData = visibleColumns.map((col) => {

            let value;

            if (col.customColumn) value = parseCustomColumn(row, col, localizedColumns, lang, fieldMapper, foreignTables)
            else value = parseColumn(col, foreignKeys, fieldMapper, row, foreignTables)

            return parseRawValue(value, col, localizedColumns, lang)
        })

        csvRows.push(csvRowData.join(","))
    })

    return csvRows.join("\n")
}

// ISO 8601 https://stackoverflow.com/a/49332027
function toISOLocal(d) {
    var z = n => ('0' + n).slice(-2);
    var zz = n => ('00' + n).slice(-3);
    var off = d.getTimezoneOffset();
    var sign = off > 0 ? '-' : '+';
    off = Math.abs(off);

    return d.getFullYear() + '-'
        + z(d.getMonth() + 1) + '-' +
        z(d.getDate()) + 'T' +
        z(d.getHours()) + ':' +
        z(d.getMinutes()) + ':' +
        z(d.getSeconds()) + '.' +
        zz(d.getMilliseconds()) +
        sign + z(off / 60 | 0) + ':' + z(off % 60);
}

// stackoverflow's implementation https://stackoverflow.com/a/18234317
function formatUnicorn(str, ...args) {
    if (typeof str !== "string") {
        throw new TypeError("First argument must be a string");
    }

    if (!args.length)
        return str;

    var t = typeof args[0];
    var replacements = "string" === t || "number" === t
        ? Array.prototype.slice.call(args)
        : args[0];

    for (var key in replacements)
        str = str.replace(new RegExp("\\{" + key + "\\}", "gi"), replacements[key]);
    return str
}

self.onmessage = async (event) => {

    const longExportToastTimer = setTimeout(() => {
        self.postMessage({ messageType: "export processing" })
    }, 8000)

    const { payload, headers, origin } = event.data;
    const { urlValue, values, columns, foreignColumns, fileName, language } = payload;

    try {
        const rows = Array.isArray(values) && values.length ? values : await fetchRows(urlValue, headers);

        if (!rows.length) throw new Error("No Items found");

        const { foreignTables, fieldMapper } = buildForeignColumnsMapper(columns, foreignColumns, origin)

        // Populate unique filter values for foreign keys
        populateForeignFilterValues(rows, foreignTables, fieldMapper)

        // Concurrently fetch data for foreign columns
        await fetchForeignEntries(foreignTables, headers)

        generateItemMapper(foreignTables)
        const csvContent = generateCSVContent(rows, columns, foreignTables, fieldMapper, language)
        const csvURL = URL.createObjectURL(new Blob([csvContent], { type: "text/csv" }))

        clearTimeout(longExportToastTimer)

        self.postMessage({ csvURL, fileName, message: "", isSuccess: true, messageType: "export ended" })
    } catch (error) {
        clearTimeout(longExportToastTimer)

        console.error(error)
        self.postMessage({ isSuccess: false, message: error?.message || "Please try again later.", messageType: "export ended" })
    } 

};

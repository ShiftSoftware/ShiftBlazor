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

        if (!foreignTables[col.foreignEntiyField]) foreignTables[col.foreignEntiyField] = {
            items: [],
            itemsMapper: {},
            filterValues: {},
            url: (`${col.url || origin}/${col.entitySet}`).replaceAll("//", "/").replace(":/", "://")
        }

        fieldMapper[col.propertyName] = {
            table: col.foreignEntiyField,
            idKey: col.tEntityValueField,
            valueKey: col.tEntityTextField,
        }

    })

    return { foreignTables, fieldMapper }
}

function populateForeignFilterValues(rows, foreignTables, fieldMapper) {

    // if there is no foreign columns then skip populating
    if (!Object.keys(fieldMapper)) return

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
    Object.values(foreignTables).map((v) => {
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

const pad = (n) => (n < 10 ? '0' + n : n);

const replacements = {
    yyyy: (date) => date.getFullYear(),
    yy: (date) => String(date.getFullYear()).slice(-2),
    MM: (date) => pad(date.getMonth() + 1),
    M: (date) => date.getMonth() + 1,
    dd: (date) => pad(date.getDate()),
    d: (date) => date.getDate(),
    HH: (date) => pad(date.getHours()),
    H: (date) => date.getHours(),
    hh: (date) => pad(date.getHours() % 12 || 12),
    h: (date) => date.getHours() % 12 || 12,
    mm: (date) => pad(date.getMinutes()),
    m: (date) => date.getMinutes(),
    tt: (date) => date.getHours() < 12 ? 'AM' : 'PM'
};

function formatDate(date, dateFormat, timeFormat, isRTL) {
    const isForwardSlash = dateFormat.includes("/")

    let dateParts

    if (isForwardSlash) dateParts = dateFormat.split("/")
    else dateParts = dateFormat.split("-")

    const parsedDateParts = dateParts.map(part => (replacements[part] && replacements[part](date)) || part)

    const parsedDate = parsedDateParts.join(isForwardSlash ? "/" : "-")


    const [hour, rest] = timeFormat.split(":");
    const [minute, period] = rest ? rest.split(" ") : [undefined, undefined];
    const timeParts = [hour, minute, period].filter(Boolean);

    const parsedTimeParts = timeParts.map(part => (replacements[part] && replacements[part](date)) || part)

    let parsedTime = `${parsedTimeParts[0]}:${parsedTimeParts[1]}`

    if (parsedTimeParts.length > 2) {
        if (isRTL) parsedTime = `${parsedTimeParts[2]} ` + parsedTime
        else parsedTime += ` ${parsedTimeParts[2]}`
    }

    return isRTL ? `${parsedTime} ${parsedDate}` : `${parsedDate} ${parsedTime}`
}

function parseRawValue(value, col, localizedColumns, language, dateFormat, timeFormat, isRTL) {

    if (col.enumValues && typeof col.enumValues[value] === "string") {
        try {
            value = col.enumValues[value]
        } catch { }
    }
    

    if (localizedColumns.has(col.title)) { // normal texts localized
        try {
            const localizedObject = JSON.parse(value)
            value = localizedObject[language] ?? value
        } catch { }
    }

    try {
        if (col.format) {
            let tempValue = +value
            value = format(col.format, tempValue)
        }
    } catch { }


    if (value instanceof Date) {
        value = formatDate(value, dateFormat, timeFormat, isRTL)
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

    } else if (col.key.includes('.')) { // process dateTime columns
        const [columnKey, columnType] = col.key.split('.');

        value = row[columnKey]

        if (columnType === "DateTime") value = new Date(value)
    }

    return value;

}

function parseCustomColumn(row, col, localizedColumns, language, dateFormat, timeFormat, isRTL, fieldMapper, foreignTables) {
    let parsedValue = ""

    col.customColumn.forEach(({ type, value }) => {
        if (type === "string") parsedValue += value
        else if (type === "property") {
            if (value.includes(".")) {
                const [tableTarget, fieldTarget] = value.split(".")

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
                                tempValue = localizedObject[language]
                            } catch { }
                        }

                        parsedValue += parseRawValue(tempValue, col, localizedColumns, language, dateFormat, timeFormat, isRTL)
                    } catch { }


                }
            } else {
                if (row[value]) {
                    if (typeof row[value] === "string") {
                        let tempValue = row[value];

                        try {
                            const localizedObject = JSON.parse(tempValue)
                            tempValue = localizedObject[language]
                        } catch { }

                        parsedValue += parseRawValue(tempValue, col, localizedColumns, language, dateFormat, timeFormat, isRTL)

                    } else {
                        parsedValue += parseRawValue(row[value], col, localizedColumns, language, dateFormat, timeFormat, isRTL)
                    }
                }
            }
        }
    })

    return parsedValue
}

function generateCSVContent(rows, columns, language, dateFormat, timeFormat, foreignTables, fieldMapper, isRTL) {
    const csvRows = []
    const localizedColumns = new Set()
    const foreignKeys = Object.keys(fieldMapper)

    const visibleColumns = columns.filter(col => !col.hidden)

    // Header row
    csvRows.push(visibleColumns.map((c) => c.title).join(","))

    rows.forEach((row, rowIndex) => {
        const csvRowData = visibleColumns.map((col) => {

            let value;

            if (col.customColumn) value = parseCustomColumn(row, col, localizedColumns, language, dateFormat, timeFormat, isRTL, fieldMapper, foreignTables)
            else value = parseColumn(col, foreignKeys, fieldMapper, row, foreignTables)

            // Determine localized columns based on the first row
            if (rowIndex === 0) {
                try {
                    const localizedObject = JSON.parse(value)
                    if (localizedObject[language]) localizedColumns.add(col.title)
                } catch { }
            }

            return parseRawValue(value, col, localizedColumns, language, dateFormat, timeFormat, isRTL)
        })

        csvRows.push(csvRowData.join(","))
    })

    return csvRows.join("\n")
}

self.onmessage = async (event) => {
    const { payload, headers, origin } = event.data;

    const { urlValue, values, columns, fileName, language, foreignColumns, dateFormat, timeFormat, isRTL } = payload;

    try {
        const rows = Array.isArray(values) && values.length ? values : await fetchRows(urlValue, headers);

        if (!rows.length) throw new Error("No Items found");

        const { foreignTables, fieldMapper } = buildForeignColumnsMapper(columns, foreignColumns, origin)

        // Populate unique filter values for foreign keys
        populateForeignFilterValues(rows, foreignTables, fieldMapper)

        // Concurrently fetch data for foreign columns
        await fetchForeignEntries(foreignTables, headers)

        generateItemMapper(foreignTables)

        const csvContent = generateCSVContent(rows, columns, language, dateFormat, timeFormat, foreignTables, fieldMapper, isRTL)

        const csvURL = URL.createObjectURL(new Blob([csvContent], { type: "text/csv" }))

        self.postMessage({ csvURL, fileName, message: "", isSuccess: true })
    } catch (error) {
        console.error(error)
        self.postMessage({ isSuccess: false, message: error?.message || "Please try again later." })
    }

};

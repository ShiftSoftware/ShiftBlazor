async function fetchRows(url, headers) {
    const response = await fetch(url, { headers, method: "GET" })
    const data = await response.json()
    return data?.Value || []
}

function buildForeignColumnsMapper(columns, foreignColumns) {
    const mapper = {}
    const foreignKeys = []

    foreignColumns.forEach((col) => {
        // if the foreign is not in the columns ( hidden ) then skip it
        if (!columns.some((column) => column.key === col.propertyName)) return

        foreignKeys.push(col.propertyName)

        mapper[col.propertyName] = {
            entries: {},
            filterValues: [],
            rowKey: col.propertyName,
            idKey: col.tEntityValueField,
            valueKey: col.tEntityTextField,
            url: `${col.url}/${col.entitySet}`,
        }
    })

    return { foreignColumnsMapper: mapper, foreignKeys }
}

function populateForeignFilterValues(rows, foreignKeys, mapper) {

    // if there is no foreign columns then skip populating
    if (!foreignKeys.length) return

    // push unique values into filterValues
    rows.forEach((row) => {
        foreignKeys.forEach((key) => {
            const value = row[key]
            if (value && `${value}`.length && !mapper[key].filterValues.includes(value)) {
                mapper[key].filterValues.push(value)
            }
        })
    })

    // Remove foreign keys and their mapper if their mapper doeasnt have any filterValues
    for (let i = foreignKeys.length - 1; i >= 0; i--) {
        if (!mapper[foreignKeys[i]].filterValues.length) {
            delete mapper[foreignKeys[i]]
            foreignKeys.splice(i, 1)
        }
    }
}

function createODataQuery(filterType, filterValues, selectFields) {
    const quotedIds = filterValues.map((v) => `'${v}'`).join(",")
    const filterClause = `${filterType} in (${quotedIds})`
    const selectClause = selectFields.join(",")
    return `?$filter=${encodeURIComponent(filterClause)}&$select=${selectClause}`
}

function fetchForeignEntries(foreignKeys, mapper, headers) {
    const fetchPromises = foreignKeys.map((key) => {
        const foreignColumn = mapper[key]

        const url = foreignColumn.url + createODataQuery(
            foreignColumn.idKey,
            foreignColumn.filterValues,
            [foreignColumn.idKey, foreignColumn.valueKey]
        )

        return fetch(url, { headers, method: "GET" })
            .then(async (response) => {
                if (!response.ok) {
                    throw new Error(`Failed to fetch ${url}: ${response.statusText}`)
                }
                const items = (await response.json()).Value
                // create entry mapper as id : value, for example "u23" : "toyota"
                items.forEach((item) => {
                    foreignColumn.entries[item[foreignColumn.idKey]] = item[foreignColumn.valueKey]
                })
            })
    })

    return Promise.all(fetchPromises)
}

function capitalizeFirstLetter(str) {
    return str.charAt(0).toUpperCase() + str.slice(1)
}

function generateCSVContent(rows, columns, language, viableForeignKeys, mapper) {
    const languageKey = language.slice(0, 2)
    const localizedColumns = new Set()
    const csvRows = []

    // Header row
    csvRows.push(columns.map((c) => c.title).join(","))

    rows.forEach((row, rowIndex) => {
        const csvRowData = columns.map((col) => {
            let value = row[col.key]

            // If the column is foreign and it has coresponding value then replace it
            if (viableForeignKeys.includes(col.key)) {
                const foreignColumn = mapper[col.key]
                if (value && foreignColumn.entries[value]) {
                    value = foreignColumn.entries[value]
                }
            }

            // Determine localized columns based on the first row
            if (rowIndex === 0) {
                try {
                    const localizedObject = JSON.parse(value)
                    if (localizedObject[languageKey]) localizedColumns.add(col.title)
                } catch { }
            }

            // For localized columns, parse and extract the localized string
            if (localizedColumns.has(col.title)) {
                try {
                    const localizedObject = JSON.parse(value)
                    value = localizedObject[languageKey] ?? value
                } catch { }
            }

            if (value instanceof Date) {
                value = value.toISOString().replace("T", " ").split(".")[0]; // Format as 'yyyy-MM-dd HH:mm:ss'
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
        })

        csvRows.push(csvRowData.join(","))
    })

    return csvRows.join("\n")
}

self.onmessage = async (event) => {
    const { payload, headers } = event.data;
    const { urlValue, values, columns, fileName, language, foreignColumns } = payload;

    try {
        const rows = Array.isArray(values) && values.length ? values : await fetchRows(urlValue, headers);

        if (!rows.length) throw new Error("No Items found");

        const { foreignColumnsMapper, foreignKeys } = buildForeignColumnsMapper(columns, foreignColumns)

        // Populate unique filter values for foreign keys
        populateForeignFilterValues(rows, foreignKeys, foreignColumnsMapper)

        // Concurrently fetch data for foreign columns
        await fetchForeignEntries(foreignKeys, foreignColumnsMapper, headers)

        // Only use keys that have filter values and fetched entries
        const viableForeignKeys = foreignKeys.filter(
            (key) =>
                foreignColumnsMapper[key].filterValues.length &&
                Object.keys(foreignColumnsMapper[key].entries).length
        )

        const csvContent = generateCSVContent(rows, columns, language, viableForeignKeys, foreignColumnsMapper)

        const csvURL = URL.createObjectURL(new Blob([csvContent], { type: "text/csv" }))

        self.postMessage({ csvURL, fileName, message: "", isSuccess: true })
    } catch (error) {
        console.error(error)
        self.postMessage({ isSuccess: false, message: error?.message || "Please try again later." })
    }
   
};

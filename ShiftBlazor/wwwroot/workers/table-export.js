function capitalizeFirstLetter(str) {
  return str.charAt(0).toUpperCase() + str.slice(1)
}

function createODataQuery(filterType, filterValues, selectFields) {
  const quotedIds = filterValues.map((v) => `'${v}'`).join(",")

  const filterClause = `${filterType} in (${quotedIds})`

  const selectClause = selectFields.join(",")

  const urlQuery = `?$filter=${encodeURIComponent(
    filterClause
  )}&$select=${selectClause}`

  return urlQuery
}

self.onmessage = async (event) => {
  const { payload, headers } = event.data

  const { urlValue, values, columns, fileName, language, foreignColumns } =
    payload

  try {
    let rows

    if (!!values && !!values.length) {
      rows = values
    } else {
      const tableResponse = await fetch(urlValue, {
        headers,
        method: "GET",
      })

      rows = (await tableResponse.json()).Value
    }

    if (!rows || !rows.length) throw new Error("No Items found")

    const foreignColumnsMapper = {}

    const foreignKeys = []

    foreignColumns.forEach((c) => {
      if (!columns.some((x) => x.key === c.propertyName)) return

      foreignKeys.push(c.propertyName)

      foreignColumnsMapper[c.propertyName] = {
        url: c.url + "/" + c.entitySet,
        entries: {},
        filterValues: [],
        rowKey: c.propertyName,
        idKey: c.tEntityValueField,
        valueKey: c.tEntityTextField,
      }
    })

    if (!!foreignKeys.length)
      rows.forEach((row) => {
        foreignKeys.forEach((key) => {
          if (
            row[key] &&
            !!`${row[key]}`.length &&
            !foreignColumnsMapper[key].filterValues.includes(row[key])
          )
            foreignColumnsMapper[key].filterValues.push(row[key])
        })
      })

    const fetchPromises = foreignKeys.map((key) => {
      const foreignColumn = foreignColumnsMapper[key]
      if (!foreignColumn.filterValues.length) return null

      const url =
        foreignColumn.url +
        createODataQuery(foreignColumn.idKey, foreignColumn.filterValues, [
          foreignColumn.idKey,
          foreignColumn.valueKey,
        ])

      return fetch(url, {
        headers,
        method: "GET",
      }).then(async (response) => {
        if (!response.ok) {
          throw new Error(`Failed to fetch ${url}: ${response.statusText}`)
        }

        const items = (await response.json()).Value

        items.forEach((item) => {
          foreignColumn.entries[item[foreignColumn.idKey]] =
            item[foreignColumn.valueKey]
        })

        return items
      })
    })

    await Promise.all(fetchPromises)

    const viableForeignColumnKeys = foreignKeys.filter(
      (key) =>
        !!foreignColumnsMapper[key].filterValues.length &&
        !!Object.keys(foreignColumnsMapper[key].entries).length
    )

    const languageKey = language.slice(0, 2)

    const localizedColumns = []

    const csvRows = []

    csvRows.push(columns.map((c) => c.title).join(",")) // Header

    rows.forEach((row, rowIndex) => {
      let csvRowData = []

      columns.forEach((c) => {
        let value = row[c.key]

        if (viableForeignColumnKeys.includes(c.key)) {
          const foreignColumn = foreignColumnsMapper[c.key]
          if (value && foreignColumn.entries[value])
            value = foreignColumn.entries[value]
        }

        if (rowIndex === 0) {
          try {
            const localizedObject = JSON.parse(value)

            if (localizedObject[languageKey]) localizedColumns.push(c.title)
          } catch (error) {}
        }

        if (localizedColumns.includes(c.title)) {
          try {
            const localizedObject = JSON.parse(value)

            value = localizedObject[languageKey]
          } catch (error) {}
        }

        if (value instanceof Date)
          value = value.toISOString().replace("T", " ").split(".")[0]
        // Format as 'yyyy-MM-dd HH:mm:ss'
        else if (typeof value === "boolean")
          value = capitalizeFirstLetter(JSON.stringify(value))

        // Special strings

        if (
          typeof value === "string" &&
          (value.includes(",") || value.includes("\n") || value.includes('"'))
        ) {
          value = `"${value.replace(/"/g, '""')}"` // Escape double quotes
        }

        csvRowData.push(value)
      })

      csvRows.push(csvRowData.join(","))
    })

    const csvContent = csvRows.join("\n")

    const blob = new Blob([csvContent], { type: "text/csv" })
    const csvURL = URL.createObjectURL(blob)

    self.postMessage({
      csvURL,
      fileName,
      message: "",
      isSuccess: true,
    })
  } catch (error) {
    console.log(error)
    self.postMessage({
      isSuccess: false,
      message: error?.message || "Please try again later.",
    })
  }
}

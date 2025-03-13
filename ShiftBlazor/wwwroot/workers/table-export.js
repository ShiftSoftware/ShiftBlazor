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

  const { url, Values, FileName, foreignColumns, columns } = payload

  console.log(payload)

  try {
    let rows

    if (!!Values && !!Values.length) {
      rows = Values
    } else {
      const tableResponse = await fetch(url, {
        headers,
        method: "GET",
      })

      rows = (await tableResponse.json()).Value
    }

    if (!rows || !rows.length) throw new Error("No Items found")

    const foreignColumnsMaapper = {}

    const foreignKeys = []

    foreignColumns.forEach((c) => {
      foreignKeys.push(c.propertyName)

      foreignColumnsMaapper[c.propertyName] = {
        url: c.url + "/" + c.entitySet,
        entries: {},
        filterValues: [],
        rowKey: c.propertyName,
        idKey: c.tEntityValueField,
        valueKey: c.tEntityTextField,
      }
    })

    rows.forEach((row) => {
      foreignKeys.forEach((key) => {
        if (
          row[key] &&
          !!`${row[key]}`.length &&
          !foreignColumnsMaapper[key].filterValues.includes(row[key])
        )
          foreignColumnsMaapper[key].filterValues.push(row[key])
      })
    })

    const fetchPromises = foreignKeys.map((key) => {
      const foreignColumn = foreignColumnsMaapper[key]
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
        !!foreignColumnsMaapper[key].filterValues.length &&
        !!Object.keys(foreignColumnsMaapper[key].entries).length
    )

    const columnKeys = columns.map((c) => c.key)

    const columnTitles = columns.map((c) => c.title)

    console.log(columnKeys)

    console.log(columnTitles)

    const refinedRows = rows.map((row) => {
      viableForeignColumnKeys.forEach((key) => {
        const foreignColumn = foreignColumnsMaapper[key]
        if (row[key] && foreignColumn.entries[row[key]])
          row[key] = foreignColumn.entries[row[key]]
      })

      return row
    })

    console.log(refinedRows)

    self.postMessage({
      isSuccess: true,
      message: "doneeeee",
      items: [rows[0], rows[1]],
    })
  } catch (error) {
    console.log(error)
    self.postMessage({
      isSuccess: false,
      message: error?.message || "Please try again later.",
      items: [],
    })
  }
}

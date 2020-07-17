// A set of APIs that wrap the Spotfire API in a way to support our SpotfireTestDriver
// We assume that we only open a single analysis instance per page (that's how the driver is intended to work), so we can do things like cache the data table objects
(function (window) {
    "use strict"
    var _app;
    var _isOpened = false;
    var _errors = [];
    var _dataTables = null;
    var _markingNames = null;
    var _dataTypesCannotFetchData = ["Binary", "Boolean", "Date", "DateTime", "LongInteger", "Real"]

    // Callback when Spotfire API has an error
    function _onErrorCallback(errorcode, description) {
        _errors.push(description);
    }

    // Callback when Spotfire completes opening the file
    function _onOpenedCallback() {
        _isOpened = true;
    }

    // Return the (potentially cached) data tables via a promise
    function _getDataTables() {
        return new Promise(function (resolve) {
            if (!_dataTables) {
                _app.analysisDocument.data.getDataTables(function (dataTables) {
                    _dataTables = {}
                    $.each(dataTables, function (index, table) {
                        _dataTables[table.dataTableName] = table;
                    });
                    resolve(_dataTables);
                });
            }
            else {
                resolve(_dataTables);
            }
        });
    }

    // Return the (potentially cached) properties for a table via a promise
    function _getDataTableProperties(tableName) {
        return _getDataTables()
            .then(function (dataTables) {
                return new Promise(function (resolve) {
                    var table = dataTables[tableName];
                    if (!table) {
                        resolve({});
                    }
                    if (!table.hasOwnProperty("cachedProperties")) {
                        table.getDataTableProperties(function (properties) {
                            table.cachedProperties = {}
                            $.each(properties, function (i, property) {
                                table.cachedProperties[property.name] = property.value;
                            });
                            resolve(table.cachedProperties);
                        })
                    } else {
                        resolve(table.cachedProperties);
                    }
                });
            });
    }

    // Return the (potentially cached) list of columns for a table via a promise
    function _getColumns(tableName) {
        return _getDataTables()
            .then(function (dataTables) {
                return new Promise(function (resolve) {
                    var table = dataTables[tableName];
                    if (!table) {
                        resolve({});
                    }
                    if (!table.hasOwnProperty("cachedColumns")) {
                        table.getDataColumns(function (dataTableColumns) {
                            table.cachedColumns = {}
                            $.each(dataTableColumns, function (i, column) {
                                table.cachedColumns[column.dataColumnName] = column;
                            });
                            resolve(table.cachedColumns);
                        })
                    } else {
                        resolve(table.cachedColumns);
                    }
                });
            });
    }

    // Return the (potentially cached) list of properties for a table column via a promise
    function _getColumnProperties(tableName, columnName) {
        return _getColumns(tableName)
            .then(function (columns) {
                return new Promise(function (resolve) {
                    var column = columns[columnName];
                    if (!column) {
                        resolve({});
                    }
                    if (!column.hasOwnProperty("cachedProperties")) {
                        column.getDataColumnProperties(function (properties) {
                            column.cachedProperties = {}
                            $.each(properties, function (i, property) {
                                column.cachedProperties[property.name] = property.value;
                            });
                            resolve(column.cachedProperties);
                        })
                    } else {
                        resolve(column.cachedProperties);
                    }
                });
            });
    }

    // Return the (potentially cached) marking names via a promise
    function _getMarkingNames() {
        return new Promise(function (resolve) {
            if (!_markingNames) {
                _app.analysisDocument.marking.getMarkingNames(function (markingNames) {
                    _markingNames = [];
                    $.each(markingNames, function (index, markingName) {
                        _markingNames.push(markingName);
                    });
                    resolve(_markingNames);
                });
            }
            else {
                resolve(_markingNames);
            }
        });
    }

    window.SpotfireTestWrapper = {
        setCredentials: function(username, password) {
            // Send the credentials to our extension
            console.log('Sending credentials to extension for user ', username)
            window.postMessage( { type: "SET_CREDENTIALS", username: username, password: password }, "*")
        },
        startSpotfire: function (serverURL, file, configurationBlock) {
            // An API for starting Spotfire from a specific URL - fetches the Spotfire API and opens the file
            $.getScript(serverURL + "/spotfire/wp/GetJavaScriptApi.ashx?Version=7.11", function () {
                var customization = new spotfire.webPlayer.Customization();
                _app = new spotfire.webPlayer.Application(serverURL + '/spotfire/wp/', customization, file, configurationBlock);
                _app.onOpened(_onOpenedCallback);
                _app.onError(_onErrorCallback);
                _app.openDocument('SpotfireContent');
            });
        },
        isOpened: function () {
            // Check if Spotfire has completed opening the file
            return _isOpened;
        },
        hadError: function () {
            // Check if we've had an error
            return _errors.length > 0;
        },
        popErrors: function () {
            // Return any API errors and clear them
            var answer = _errors;
            _errors = [];
            return answer;
        },
        get application() {
            // The Spotfire application
            return _app;
        },
        tableNames: function (callback) {
            // the callback will receive an array of table names
            _getDataTables().then(function (dataTables) {
                var tableNames = [];
                $.each(dataTables, function (i, table) {
                    tableNames.push(table.dataTableName);
                });

                callback(tableNames);
            });
        },
        tableProperties: function (tableName, callback) {
            // The callback will receive an array of property objects (name/value pairs)
            _getDataTableProperties(tableName).then(function (properties) {
                callback(properties);
            });
        },
        tableColumnNames: function (tableName, callback) {
            // the callback will receive an array of column names for the table
            _getColumns(tableName).then(function (dataTableColumns) {
                var columnNames = [];
                $.each(dataTableColumns, function (i, column) {
                    columnNames.push(column.dataColumnName);
                });

                callback(columnNames);
            });
        },
        columnDataType: function (tableName, columnName, callback) {
            // The callback will receive the column type
            _getColumns(tableName).then(function (columns) {
                var column = columns[columnName];
                if (!column) {
                    callback("");
                }
                callback(column.dataType);
            });
        },
        columnProperties: function (tableName, columnName, callback) {
            // The callback will receive an array of properties for the column
            _getColumnProperties(tableName, columnName).then(function (properties) {
                callback(properties);
            });
        },
        columnDistinctValueCount: function (tableName, columnName, callback) {
            // The callback will receive the count of distinct values in the column
            // Note that the Spotfire API doesn't support this certain data types (presumably because the API is intended for use with calls for filtering)
            _getColumns(tableName).then(function (columns) {
                var column = columns[columnName];
                if (!column) {
                    callback(-1);
                }
                if (!_dataTypesCannotFetchData.includes(column.dataType)) {
                    column.getDistinctValues(0, 1, function (result) {
                        callback(result.count);
                    });
                } else {
                    callback(-1);
                }
            });
        },
        columnDistinctValues: function (tableName, columnName, startIndex, responseLimit, callback) {
            // The callback will receive an array of distinct values from the column. the startIndex and responseLimit parameters allow values to be fetched in pages
            // Note that the Spotfire API doesn't support this certain data types (presumably because the API is intended for use with calls for filtering)
           _getColumns(tableName).then(function (columns) {
               var column = columns[columnName];
               if (!column) {
                   callback(["column doesn't exist"]);
               }
               if (!_dataTypesCannotFetchData.includes(column.dataType)) {
                   column.getDistinctValues(startIndex, responseLimit, function (result) {
                       callback(result.values);
                   });
               } else {
                   callback([]);
               }
            });
        },
        pages: function (callback) {
            // The callback will receive an array of page names
            _app.analysisDocument.getPages(function (pages) {
                var answer = [];
                $.each(pages, function (i, page) {
                    answer.push(page.pageTitle);
                });
                callback(answer);
            });
        },
        markingNames: function (callback) {
            // The callback will receive an array of marking names
            _getMarkingNames().then(callback)
        },
        clearAllMarkings: function (callback) {
            // Clear all markings, calls the callback when done
            _getDataTables().then(function (datatables) {
                _getMarkingNames().then(function (markingNames) {
                    $.each(markingNames, function (i, markingName) {
                        $.each(datatables, function (i, datatable) {
                            _app.analysisDocument.marking.setMarking(markingName, datatable.dataTableName, "", spotfire.webPlayer.markingOperation.CLEAR);
                        });
                    });
                    callback();
                });
            });
        }
    }
})(window)


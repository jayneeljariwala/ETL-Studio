(function () {
    const form = document.getElementById("etlJobForm");
    const body = document.getElementById("mappingTableBody");
    const template = document.getElementById("mappingRowTemplate");
    const addButton = document.getElementById("addMappingBtn");
    const helpAlert = document.getElementById("customExpressionHelp");

    if (!form || !body || !template || !addButton) {
        return;
    }

    let dragSource = null;

    function checkCustomExpressionSelection() {
        if (!helpAlert) return;
        const selects = Array.from(body.querySelectorAll("select"));
        // TransformationType.CustomExpression is typically 5
        const hasCustom = selects.some(s => s.value === "5" || s.value === "CustomExpression");
        if (hasCustom) {
            helpAlert.classList.remove("d-none");
        } else {
            helpAlert.classList.add("d-none");
        }
    }

    function setRowIndexing() {
        const rows = Array.from(body.querySelectorAll("tr.mapping-row"));
        rows.forEach((row, index) => {
            row.querySelectorAll("[name]").forEach((input) => {
                input.name = input.name
                    .replace(/FieldMappings\[\d+\]/g, `FieldMappings[${index}]`)
                    .replace(/Transformations\[\d+\]/g, "Transformations[0]");
            });

            row.querySelectorAll(".mapping-order").forEach((orderInput) => {
                orderInput.value = index.toString();
            });
        });
    }

    function wireRow(row) {
        row.addEventListener("dragstart", () => {
            dragSource = row;
            row.classList.add("table-warning");
        });

        row.addEventListener("dragend", () => {
            dragSource = null;
            row.classList.remove("table-warning");
            setRowIndexing();
        });

        row.addEventListener("dragover", (e) => {
            e.preventDefault();
        });

        row.addEventListener("drop", (e) => {
            e.preventDefault();
            if (!dragSource || dragSource === row) {
                return;
            }

            const rows = Array.from(body.querySelectorAll("tr.mapping-row"));
            const sourceIndex = rows.indexOf(dragSource);
            const targetIndex = rows.indexOf(row);

            if (sourceIndex < targetIndex) {
                body.insertBefore(dragSource, row.nextSibling);
            } else {
                body.insertBefore(dragSource, row);
            }

            setRowIndexing();
        });

        const removeButton = row.querySelector(".remove-mapping");
        if (removeButton) {
            removeButton.addEventListener("click", () => {
                row.remove();
                if (!body.querySelector("tr.mapping-row")) {
                    addMappingRow();
                }
                setRowIndexing();
                checkCustomExpressionSelection();
            });
        }
    }

    function addMappingRow() {
        const nextIndex = body.querySelectorAll("tr.mapping-row").length;
        const fragment = template.content.cloneNode(true);
        const row = fragment.querySelector("tr.mapping-row");

        row.querySelectorAll("[data-name]").forEach((element) => {
            const name = element.getAttribute("data-name").replace(/__index__/g, nextIndex.toString());
            element.setAttribute("name", name);
        });

        wireRow(row);
        body.appendChild(row);
        setRowIndexing();
    }

    addButton.addEventListener("click", addMappingRow);

    body.querySelectorAll("tr.mapping-row").forEach(wireRow);
    setRowIndexing();
    checkCustomExpressionSelection();

    body.addEventListener("change", function(e) {
        if (e.target.tagName === "SELECT") {
            checkCustomExpressionSelection();
        }
    });

    form.addEventListener("submit", setRowIndexing);
})();

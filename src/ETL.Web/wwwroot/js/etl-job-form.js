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

        body.querySelectorAll("tr.mapping-row").forEach(row => {
            const select = row.querySelector("select");
            const btn = row.querySelector(".code-editor-btn");
            const input = row.querySelector(".parameter-input");
            if (select && btn && input) {
                if (select.value === "5" || select.value === "CustomExpression") {
                    btn.classList.remove("d-none");
                    input.readOnly = true;
                } else {
                    btn.classList.add("d-none");
                    input.readOnly = false;
                }
            }
        });
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

    // --- Monaco Editor Logic ---
    let editorInstance = null;
    let currentInputTarget = null;
    const codeEditorModalEl = document.getElementById("codeEditorModal");
    let codeEditorModal;

    if (codeEditorModalEl) {
        codeEditorModal = new bootstrap.Modal(codeEditorModalEl);
        
        // Fix for Monaco editor rendering issue in Bootstrap Modal
        codeEditorModalEl.addEventListener("shown.bs.modal", () => {
            if (editorInstance) {
                editorInstance.layout();
                editorInstance.focus();
            }
        });
    }
    
    // Delegate click for code-editor-btn
    body.addEventListener("click", function(e) {
        const btn = e.target.closest(".code-editor-btn");
        if (!btn) return;
        
        currentInputTarget = btn.closest(".input-group").querySelector(".parameter-input");
        const currentCode = currentInputTarget.value || "";
        
        if (!editorInstance) {
            if (window.monacoEditorReady) {
                initMonacoAndShow(currentCode);
            } else {
                window.onMonacoReady = () => initMonacoAndShow(currentCode);
            }
        } else {
            editorInstance.setValue(currentCode);
            codeEditorModal.show();
        }
    });

    function initMonacoAndShow(code) {
        if (!editorInstance) {
            editorInstance = monaco.editor.create(document.getElementById("monacoEditorContainer"), {
                value: code,
                language: 'csharp',
                theme: 'vs-dark',
                automaticLayout: true,
                minimap: { enabled: false }
            });
        } else {
            editorInstance.setValue(code);
        }
        codeEditorModal.show();
    }

    const saveCodeBtn = document.getElementById("saveCodeBtn");
    if (saveCodeBtn) {
        saveCodeBtn.addEventListener("click", function() {
            if (currentInputTarget && editorInstance) {
                currentInputTarget.value = editorInstance.getValue();
            }
            if (codeEditorModal) {
                codeEditorModal.hide();
            }
        });
    }
})();

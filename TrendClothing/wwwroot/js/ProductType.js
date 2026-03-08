var dataTable;
$(document).ready(function () { LoadDataTable(); });

function LoadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { "url": "/Admin/ProductType/GetAll" },
        "columns": [
            {
                "data": "name",
                "render": function (data, type, row) {
                    return `<span style="font-weight:600;">${data}</span>
                            <span style="font-size:11px;color:var(--tc-gray-3);margin-left:6px;">(${row.categories})</span>`;
                }
            },
            {
                "data": "id",
                "render": data =>
                    `<div style="display:flex;gap:8px;">
                        <a href="/Admin/ProductType/Upsert/${data}" class="tc-admin-btn-edit">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                            Edit
                        </a>
                        <button class="tc-admin-btn-delete" onclick="Delete('/Admin/ProductType/Delete/${data}')">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4h6v2"/></svg>
                            Delete
                        </button>
                    </div>`
            }
        ]
    });
}

function Delete(url) {
    swal({ title: "Delete product type?", icon: "warning", buttons: ["Cancel", "Delete"], dangerMode: true })
        .then(ok => {
            if (ok) {
                $.ajax({
                    url: url, type: "DELETE", success: data => {
                        data.success ? toastr.success(data.message) : toastr.error(data.message);
                        dataTable.ajax.reload();
                    }
                });
            }
        });
}
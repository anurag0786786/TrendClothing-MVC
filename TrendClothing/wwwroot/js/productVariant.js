var dataTable;

$(document).ready(function () { loadDataTable(); });

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { "url": "/Admin/ProductVariant/GetAll" },
        "columns": [
            {
                "data": "product",
                "render": data => `<span style="font-weight:600;">${data}</span>`
            },
            { "data": "size" },
            { "data": "color" },
            {
                "data": "price",
                "render": data => `<span style="font-weight:700;">&#8377; ${data}</span>`
            },
            {
                // ✅ Inline stock edit
                "data": "stock",
                "render": (data, type, row) => {
                    const badgeClass = data === 0
                        ? 'tc-status-inactive'
                        : data <= 5
                            ? 'style="background:#fffbeb;color:#b5830a;padding:3px 8px;border-radius:20px;font-size:11px;font-weight:700;"'
                            : 'tc-status-active';

                    const badgeStyle = data === 0
                        ? `<span class="tc-status-inactive">Out of stock</span>`
                        : data <= 5
                            ? `<span style="background:#fffbeb;color:#b5830a;padding:3px 8px;border-radius:20px;font-size:11px;font-weight:700;">⚠️ ${data} left</span>`
                            : `<span class="tc-status-active">${data} in stock</span>`;

                    return `<div style="display:flex;align-items:center;gap:8px;">
                        ${badgeStyle}
                        <button class="tc-admin-btn-edit" style="font-size:10px;padding:4px 10px;"
                            onclick="quickUpdateStock(${row.id}, ${data})">
                            Update
                        </button>
                    </div>`;
                }
            },
            {
                "data": "id",
                "render": data =>
                    `<div style="display:flex;gap:8px;">
                        <a href="/Admin/ProductVariant/Edit/${data}" class="tc-admin-btn-edit">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                            Edit
                        </a>
                        <button class="tc-admin-btn-delete" onclick="Delete('/Admin/ProductVariant/Delete/${data}')">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4h6v2"/></svg>
                            Delete
                        </button>
                    </div>`
            }
        ]
    });
}

// ✅ Quick inline stock update
function quickUpdateStock(variantId, currentStock) {
    var newStock = prompt('Update stock for this variant:\nCurrent stock: ' + currentStock, currentStock);
    if (newStock === null) return; // cancelled

    newStock = parseInt(newStock);
    if (isNaN(newStock) || newStock < 0) {
        toastr.error('Please enter a valid stock number');
        return;
    }

    $.post('/Admin/ProductVariant/UpdateStock', { id: variantId, stock: newStock }, function (res) {
        if (res.success) {
            toastr.success(res.message);
            dataTable.ajax.reload(null, false);
        } else {
            toastr.error(res.message);
        }
    });
}

function Delete(url) {
    swal({ title: "Delete variant?", icon: "warning", buttons: ["Cancel", "Delete"], dangerMode: true })
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
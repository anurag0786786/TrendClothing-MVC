var dataTable;

$(document).ready(function () {

    LoadDataTable();

    var categoryId = $("#CategoryId").val();
    var productTypeId = $("#selectedProductTypeId").val();

    if (categoryId && categoryId != "0") {
        loadProductTypes(categoryId, productTypeId);
    }

    $("#CategoryId").change(function () {
        loadProductTypes($(this).val(), null);
    });
});

function loadProductTypes(categoryId, selectedId) {
    $("#ProductTypeId").empty();
    $("#ProductTypeId").append('<option value="">-- Select Product Type --</option>');
    if (categoryId && categoryId != "0") {
        $.get("/Admin/Product/GetProductTypesByCategory", { categoryId: categoryId }, function (data) {
            $.each(data, function (i, item) {
                $("#ProductTypeId").append(
                    `<option value="${item.id}" ${item.id == selectedId ? "selected" : ""}>${item.name}</option>`
                );
            });
        });
    }
}

function LoadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { "url": "/Admin/Product/GetAll" },
        "columns": [
            { "data": "name", "render": data => `<span style="font-weight:600;">${data}</span>` },
            { "data": "price", "render": data => `<span style="font-weight:700;">&#8377; ${data}</span>` },
            { "data": "category.name" },
            { "data": "brand.name" },
            {
                "data": "isActive",
                "render": data => data
                    ? `<span class="tc-status-active">Active</span>`
                    : `<span class="tc-status-inactive">Inactive</span>`
            },
            {
                "data": "id",
                "render": data =>
                    `<div style="display:flex;gap:8px;">
                        <a href="/Admin/Product/Upsert/${data}" class="tc-admin-btn-edit">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                            Edit
                        </a>
                        <button class="tc-admin-btn-delete" onclick="Delete('/Admin/Product/Delete/${data}')">
                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4h6v2"/></svg>
                            Delete
                        </button>
                    </div>`
            }
        ]
    });
}

function Delete(url) {
    swal({ title: "Delete product?", text: "This cannot be undone.", icon: "warning", buttons: ["Cancel", "Delete"], dangerMode: true })
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
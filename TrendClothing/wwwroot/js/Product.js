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
    $("#ProductTypeId").append('<option value="0">-- Select Product Type --</option>');

    if (categoryId && categoryId != "0") {
        $.get("/Admin/Product/GetProductTypesByCategory",
            { categoryId: categoryId },
            function (data) {
                $.each(data, function (i, item) {
                    $("#ProductTypeId").append(
                        `<option value="${item.id}" ${item.id == selectedId ? "selected" : ""}>
                            ${item.name}
                        </option>`
                    );
                });
            }
        );
    }
}

function LoadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { "url": "/Admin/Product/GetAll" },
        "columns": [
            { "data": "name" },
            { "data": "price" },
            { "data": "category.name" },
            { "data": "brand.name" },
            {
                "data": "isActive",
                "render": data => data ? "Active" : "Inactive"
            },
            {
                "data": "id",
                "render": data =>
                    `<a href="/Admin/Product/Upsert/${data}" class="btn btn-info btn-sm">
                        <i class="fas fa-edit"></i>
                     </a>
                     <a class="btn btn-danger btn-sm" onclick=Delete('/Admin/Product/Delete/${data}')>
                        <i class="fas fa-trash-alt"></i>
                     </a>`
            }
        ]
    });
}

function Delete(url) {
    swal({
        title: "Want to delete?",
        icon: "warning",
        buttons: true,
        dangerMode: true
    }).then(ok => {
        if (ok) {
            $.ajax({
                url: url,
                type: "DELETE",
                success: data => {
                    data.success
                        ? toastr.success(data.message)
                        : toastr.error(data.message);
                    dataTable.ajax.reload();
                }
            });
        }
    });
}

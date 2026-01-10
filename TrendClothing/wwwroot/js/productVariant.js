var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/ProductVariant/GetAll"
        },
        "columns": [
            { "data": "product" },
            { "data": "size" },
            { "data": "color" },
            { "data": "price" },
            { "data": "stock" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <a href="/Admin/ProductVariant/Edit/${data}"
                               class="btn btn-success mx-1">
                                <i class="fas fa-edit"></i>
                            </a>
                            <a onclick=Delete('/Admin/ProductVariant/Delete/${data}')
                               class="btn btn-danger mx-1">
                                <i class="fas fa-trash"></i>
                            </a>
                        </div>`;
                }
            }
        ]
    });
}

function Delete(url) {
    swal({
        title: "Are you sure?",
        text: "Once deleted, you cannot recover!",
        icon: "warning",
        buttons: true,
        dangerMode: true
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}

<template>
  <div class="page-panel" v-loading="loading">
    <div class="header-bar">
      <el-button-group>
        <el-button icon="el-icon-plus" size="mini" type="primary" @click="handleAdd">添加</el-button>
        <el-button icon="el-icon-delete" size="mini" type="danger" @click="handleDelete">删除</el-button>
      </el-button-group>
    </div>
    <el-table
      :data="data_list"
      stripe
      row-key="Id"
      ref="table"
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" width="55"></el-table-column>
      <el-table-column prop="DictName" label="角色名称"></el-table-column>
      <el-table-column prop="DictDescribe" label="描述"></el-table-column>
      <el-table-column label="操作">
        <template slot-scope="scope">
          <el-button-group>
            <el-button
              icon="el-icon-edit"
              size="mini"
              @click="handleEdit(scope.$index, scope.row)"
            >编辑</el-button>
          </el-button-group>
        </template>
      </el-table-column>
    </el-table>
    <div class="footer-bar" text-right>
      <el-pagination
        class="pagination"
        @size-change="handleSizeChange"
        @current-change="currentChange"
        :current-page="pageInfo.PageNum"
        :page-sizes="page_sizes"
        :page-size="pageInfo.PageSize"
        layout="total, sizes, prev, pager, next, jumper"
        :total="pageInfo.total"
      ></el-pagination>
    </div>
    <el-dialog title="系统字典" :visible.sync="dialogFormVisible" @open="dialogOpened">
      <form-generator :value="value" @change="onFormCtrlChange" ref="form" :settings="formSettings"></form-generator>
      <div slot="footer" class="dialog-footer">
        <el-button @click="dialogFormVisible = false">取 消</el-button>
        <el-button type="primary" @click="onInfoSubmit">确 定</el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script>
import formSettings ,{defaultValue} from "./form_settings";
import { mapActions, mapState } from "vuex";
const dict_key = "user_role";
export default {
  name: "role-list",
  data() {
    return {
      dialogFormVisible: false,
      formSettings: Object.assign({}, formSettings),
      value: Object.assign({}, defaultValue),
      data_list: [],
      pageInfo: {},
      select_ids: []
    };
  },
  computed: {
    ...mapState("global", ["page_sizes"]),
    ...mapState("dict", ["loading"])
  },
  created() {
    this.pageInfo = Object.assign({}, this.$store.state.global.defaultPageInfo);
    this.pageInfo.QueryString = `DictType==0 and GroupKey=="${dict_key}"`;
    this.pageInfo.OrderBy = "OrderId asc";
    this.dataLoad();
  },

  methods: {
    ...mapActions("dict", ["getList", "save", "del", "getById"]),
    currentChange(ev) {
      let pageInfo = this.pageInfo;
      pageInfo.PageNum = ev;
      this.getList(pageInfo);
    },
    handleSizeChange(ev) {
      let pageInfo = this.pageInfo;
      pageInfo.PageSize = ev;
      this.getList(pageInfo);
    },

    handleSelectionChange(rows) {
      this.select_ids = rows.map(x => x.Id);
    },
    dataLoad() {
      this.getList(this.pageInfo).then(x => {
        this.data_list = x.Records;
        let pinfo = this.pageInfo;
        pinfo.total = x.Count;
        this.pageInfo = pinfo;
      });
    },
    handleEdit(index, row) {
      this.getById(row.Id).then(x => {
        this.value = x;
        this.value.DictStatus = x.DictStatus == 1 ? true : false;
        this.dialogFormVisible = true;
      });
    },
    handleDelete(index, row) {
      if (this.select_ids.length == 0) {
        this.$message({
          type: "error",
          message: "请先选择要删除的数据"
        });
        return;
      }
      this.$confirm(`是否要删除(${this.select_ids.length})条记录吗?`, "提示", {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning"
      })
        .then(() => {
          this.del(this.select_ids).then(result => {
            if (result > 0) {
              this.$message({
                type: "success",
                message: "删除成功!"
              });
              this.dataLoad();
            }
          });
        })
        .catch(() => {
          this.$message({
            type: "info",
            message: "已取消删除"
          });
        });
    },
    handleAdd() {
      this.value = Object.assign({}, defaultValue);

      this.dialogFormVisible = true;
    },
    dialogOpened() {
      let form = this.$refs["form"];
      if (!form) {
        setTimeout(() => {
          this.dialogOpened();
        }, 100);
        return;
      }
      form.resetForm();
      this.value.GroupKey = dict_key;
      form.setValues(Object.assign({}, this.value));
    },
    onInfoSubmit() {
      let vals = this.$refs["form"].formSubmit();
      if (!vals) {
        return;
      }
      vals.DictStatus = vals.DictStatus ? 1 : 0;
      this.save(vals).then(x => {
        if (x.Id > 0) {
          this.dialogFormVisible = false;
          this.dataLoad();
        }
      });
    },

    //表单项改动事件
    onFormCtrlChange(ev) {}
  }
};
</script>

<style lang="scss">
.data-table::before {
  height: 0;
}
</style>

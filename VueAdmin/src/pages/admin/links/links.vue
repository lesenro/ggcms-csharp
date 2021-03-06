<template>
  <div class="page-panel" v-loading="loading">
    <el-row type="flex" class="header-bar" justify="space-between">
      <el-col :span="8">
        <el-button-group>
          <el-button icon="el-icon-plus" size="mini" type="primary" @click="handleAdd">添加</el-button>
          <el-button icon="el-icon-delete" size="mini" type="danger" @click="handleDelete">删除</el-button>
        </el-button-group>
      </el-col>
      <el-col :span="8">
        <el-form size="mini" :inline="true" class="float-right">
          <el-form-item required>
            <el-input v-model="searchKey" clearable @clear="clearSearch" placeholder="查询关键词"></el-input>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="onSearch">查询</el-button>
          </el-form-item>
        </el-form>
      </el-col>
    </el-row>
    <el-table
      :data="data_list"
      stripe
      row-key="Id"
      ref="table"
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" width="55"></el-table-column>
      <el-table-column prop="WebName" label="网站名称">
        <template slot-scope="scope">
          {{scope.row.WebName}}
          <img :style="{height:'20px',transform:'translateY(3px)'}" v-if="scope.row.LogoImg" alt="logo img" :src="scope.row.LogoImg"/>
        </template>
      </el-table-column>
      <el-table-column prop="Url" label="网站地址"></el-table-column>
      <el-table-column prop="GroupKey" label="链接类型" width="200">
        <template slot-scope="scope">
          <el-button
            size="mini"
            type="primary"
            plain
            @click="searchByGroup(scope.row.LinkType)"
          >{{getGroupName(scope.row.LinkType)}}</el-button>
        </template>
      </el-table-column>
      <el-table-column prop="Status" label="状态" width="120">
        <template slot-scope="scope">
          <el-tag v-if="scope.row.Status==1" type="success">正常</el-tag>
          <el-tag v-if="scope.row.Status==0" type="danger">禁用</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="120">
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
    <el-dialog title="友情链接" :visible.sync="dialogFormVisible" @open="dialogOpened">
      <form-generator :value="value" @change="onFormCtrlChange" ref="form" :settings="formSettings"></form-generator>
      <div slot="footer" class="dialog-footer">
        <el-button @click="dialogFormVisible = false">取 消</el-button>
        <el-button type="primary" @click="onInfoSubmit">确 定</el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script>
import formSettings, { defaultValue } from "./form_settings";
import { mapActions, mapState } from "vuex";
export default {
  name: "link-list",
  data() {
    return {
      dialogFormVisible: false,
      formSettings: {},
      value: Object.assign({}, defaultValue),
      data_list: [],
      pageInfo: {},
      link_types: [],
      select_ids: [],
      files: [],
      searchKey:""
    };
  },
  computed: {
    ...mapState("global", ["page_sizes"]),
    ...mapState("link", ["loading"])
  },
  async created() {
    let settings = Object.assign({}, formSettings);
    let upload = settings.layouts[0].controls.find(
      x => x.key == "LogoImg"
    );
    upload.controlProps.httpRequest = ev => this.onFileSelect(ev, "LogoImg");
    this.formSettings = settings;
    let grps = await this.getDictList({
      QueryString: 'GroupKey=="link_type" and DictType==1 and DictStatus=1',
      PageSize: 0,
      OrderBy: "OrderId asc"
    });
    if (grps.Records.length > 0) {
      this.link_types = grps.Records.map(x => {
        return {
          label: x.DictName,
          value: x.DictKey
        };
      });
    }
    this.pageInfo = Object.assign({}, this.$store.state.global.defaultPageInfo);
    this.pageInfo.QueryString = "";
    this.pageInfo.OrderBy = "OrderID asc";
    this.dataLoad();
  },

  methods: {
    ...mapActions("dict", {
      getDictList: "getList"
    }),
    ...mapActions("link", ["getList", "save", "del", "getById"]),
    ...mapActions("global", ["fileUpload"]),
    currentChange(ev) {
      let pageInfo = this.pageInfo;
      pageInfo.PageNum = ev;
      this.dataLoad();
    },
    handleSizeChange(ev) {
      let pageInfo = this.pageInfo;
      pageInfo.PageSize = ev;
      this.dataLoad();
    },
    getGroupName(gkey) {
      let grp = this.link_types.find(x => x.value == gkey);
      if (grp) {
        return grp.label;
      }
      return "";
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
    onFileSelect(ev, key) {
      const form = this.$refs["form"];
      let ctrl = form.getControl(key);
      if (ev.file) {
        if (!ev.file.type.startsWith("image")) {
          this.$message({
            type: "error",
            message: "必须上传图片"
          });
          ctrl.clearFiles();
          return;
        }
        this.fileUpload({
          type: "link",
          file: ev.file
        }).then(x => {
          let file = this.files.find(f => f.propertyName == key);
          if (file) {
            file.filePath = x.Data[0].url;
            file.propertyName = key;
            file.fileType = 0;
          } else {
            this.files.push({
              filePath: x.Data[0].url,
              propertyName: key,
              fileType: 0
            });
          }
          ctrl.clearFiles();
          form.setValue(key, x.link);
        });
      }
      //
    },
    handleEdit(index, row) {
      this.getById(row.Id).then(x => {
        this.value = x;
        this.value.Status = x.Status == 1 ? true : false;
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
      this.files=[];
      form.setOptions("LinkType", this.link_types);

      form.setValues(Object.assign({}, this.value));
    },
    onInfoSubmit() {
      let vals = this.$refs["form"].formSubmit();
      if (!vals) {
        return;
      }
      vals.files = this.files;
      vals.Status = vals.Status ? 1 : 0;
      this.save(vals).then(x => {
        if (x.Id > 0) {
          this.dialogFormVisible = false;
          this.dataLoad();
        }
      });
    },

    //表单项改动事件
    onFormCtrlChange(ev) {},
    searchByGroup(gid) {
      if (!gid) {
        return;
      }
      this.searchKey = "g:" + this.getGroupName(gid);
      this.onSearch();
    },
    onSearch() {
      if (!this.searchKey) {
        return;
      }
      let rule = /^g:/gi;
      if (rule.test(this.searchKey)) {
        let group = this.searchKey.replace(rule, "");
        let citem = this.link_types.find(
          x => x.label.indexOf(group) != -1
        );
        if (citem) {
          this.pageInfo.QueryString = `LinkType=="${citem.value}"`;
        } else {
          return;
        }
      } else {
        this.pageInfo.QueryString = `(WebName.Contains("${this.searchKey}") or LinkType.Contains("${this.searchKey}") or Url.Contains("${this.searchKey}"))`;
      }
      this.pageInfo.PageNum = 1;
      this.dataLoad();
    },
    clearSearch() {
      this.searchKey = "";
      this.pageInfo.QueryString = ``;
      this.pageInfo.PageNum = 1;
      this.dataLoad();
    }
  }
};
</script>

<style lang="scss">
.data-table::before {
  height: 0;
}
</style>

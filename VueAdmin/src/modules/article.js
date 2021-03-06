
import {
    articleSave,
    articleDel,
    articleGetById,
    articleGetList,
    modulesGetValue,
    getPageInfo
} from '../platformApi';
const articleModule = {
    namespaced: true,
    state: {
        loading: false,
    },
    mutations: {
        setLoading(state, val) {
            state.loading = val;
        },
    },
    actions: {
        async getList(ctx, params) {
            ctx.commit("setLoading", true);
            let nParams = Object.assign({}, params);
            let result = await articleGetList(nParams);
            ctx.commit("setLoading", false);
            return result;
        },
        async save(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await articleSave(params);
            ctx.commit("setLoading", false);
            return result;
        },
        async del(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await articleDel(params);
            ctx.commit("setLoading", false);
            return result;
        },
        async getById(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await articleGetById(params);
            ctx.commit("setLoading", false);
            return result;
        },
        async getValues(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await modulesGetValue(params);
            ctx.commit("setLoading", false);
            return result;
        },
        async getPageInfo(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await getPageInfo(params);
            ctx.commit("setLoading", false);
            return result;
        },
        async getPageInfoById(ctx, params) {
            ctx.commit("setLoading", true);
            let result = await getPageInfo(params);
            ctx.commit("setLoading", false);
            return result;
        },
    }
}

export default articleModule;
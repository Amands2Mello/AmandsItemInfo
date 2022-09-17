import type { DependencyContainer } from "tsyringe";
import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { IPostAkiLoadMod } from "@spt-aki/models/external/IPostAkiLoadMod";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { DynamicRouterModService } from "@spt-aki/services/mod/dynamicRouter/DynamicRouterModService";
import { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import { IHandbookBase } from "@spt-aki/models/eft/common/tables/IHandbookBase";
import { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";

interface IItemsNeeded {
    quest: number;
    hideout: number;
}

interface IItemsProfile {
    stashRaid: number;
    stash: number;
    quest: number;
}

class ItemInfo implements IPreAkiLoadMod, IPostAkiLoadMod, IPostDBLoadMod
{
    private Config = require("../config/config.json");

    private pkg;
    private logger: ILogger;
    private database: DatabaseServer;
    private router: DynamicRouterModService;
    private http: HttpResponseUtil;
    private items: Record<string, ITemplateItem>;
    private table: IDatabaseTables;
    private livePrice;
    private handbookTable: IHandbookBase;
    private therapist;
    private ragman;
    private jaeger;
    private mechanic;
    private prapor;
    private peacekeeper;
    private skier; 
    private fence;
    private tradersArr;
    private profileHelper;
    private itemsNeeded: IItemsNeeded[] = [];
    private itemsNotNeeded: IItemsNeeded[] = [];
    private itemsProfile: IItemsProfile[] = [];
    private itemsWishlist: Number[] = [];
    private MS2000Markers: number = 0;

    preAkiLoad(container: DependencyContainer): void {
        this.pkg = require("../package.json");
        this.router = container.resolve<DynamicRouterModService>("DynamicRouterModService");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        this.logger = container.resolve<ILogger>("WinstonLogger");
        this.http = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.logger.info(`loading: ${this.pkg.author}: ${this.pkg.name} ${this.pkg.version}`);
        this.addRoute();
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        staticRouterModService.registerStaticRouter(
        "ItemInfovalidate",
        [
            {
                url: "/client/game/version/validate",
                action: (url, info, sessionId, output) => 
                {            
                    this.UpdateItemsNotNeeded(sessionId);
                    return output;
                }
            }
        ],
        "aki"
        );
        staticRouterModService.registerStaticRouter(
            "ItemInforaidmenu",
            [
                {
                    url: "/singleplayer/settings/raid/menu",
                    action: (url, info, sessionId, output) => 
                    {            
                        this.UpdateItemsNotNeeded(sessionId);
                        return output;
                    }
                }
            ],
            "aki"
        );
        staticRouterModService.registerStaticRouter(
            "ItemInfoitemsmoving",
            [
                {
                    url: "/client/game/profile/items/moving",
                    action: (url, info, sessionId, output) => 
                    {            
                        this.UpdateItemsNotNeeded(sessionId);
                        return output;
                    }
                }
            ],
            "aki"
        );
    }
    postAkiLoad(container: DependencyContainer): void {
        this.database = container.resolve<DatabaseServer>("DatabaseServer");
        this.table = this.database.getTables();
        this.items = this.table.templates.items;
        this.livePrice = this.table.templates.prices;
        this.handbookTable = this.table.templates.handbook;
        this.therapist = this.table.traders["54cb57776803fa99248b456e"].base;
        this.ragman = this.table.traders["5ac3b934156ae10c4430e83c"].base;
        this.jaeger = this.table.traders["5c0647fdd443bc2504c2d371"].base;
        this.mechanic = this.table.traders["5a7c2eca46aef81a7ca2145d"].base;
        this.prapor = this.table.traders["54cb50c76803fa8b248b4571"].base;
        this.peacekeeper = this.table.traders["5935c25fb3acc3127c3d8cd9"].base;
        this.skier = this.table.traders["58330581ace78e27b8b10cee"].base;
        this.fence = this.table.traders["579dc571d53a0658a154fbec"].base;
        this.tradersArr = [this.therapist, this.ragman, this.jaeger, this.mechanic, this.prapor, this.skier, this.peacekeeper, this.fence];
        for (let key of this.Config.keys) {
            this.itemsNeeded[key] = {quest: Number(1), hideout: Number(0)};
        }
        for (let quest in this.table.templates.quests)
        {
            for (let conditionAvailableForFinish of this.table.templates.quests[quest].conditions.AvailableForFinish) {
                if (conditionAvailableForFinish._parent == "HandoverItem" && conditionAvailableForFinish._props.onlyFoundInRaid) {
                    if (this.itemsNeeded[conditionAvailableForFinish._props.target]) {
                        this.itemsNeeded[conditionAvailableForFinish._props.target].quest +=  Number(conditionAvailableForFinish._props.value);
                    }
                    else {
                        this.itemsNeeded[conditionAvailableForFinish._props.target] = {quest: Number(conditionAvailableForFinish._props.value), hideout: Number(0)};
                    }
                }
            }
        }
        for (let area of this.table.hideout.areas) {
            for (let stage in area.stages) {
                for(let requirement of area.stages[stage].requirements) {
                    if (requirement?.type == "Item" && requirement?.templateId && requirement?.count) {
                        if (this.itemsNeeded[requirement.templateId]) {
                            this.itemsNeeded[requirement.templateId].hideout +=  Number(requirement.count);
                        }
                        else {
                            this.itemsNeeded[requirement.templateId] = {quest: Number(0), hideout: Number(requirement.count)};
                        }
                    }
                }
            }
        }
    }
    postDBLoad(container: DependencyContainer): void {
        return;
    }
    // OPTIMIZE ON UPDATE 1.1 PER ITEM CHANGES
    private UpdateItemsNotNeeded(sessionId: string) : void
    {
        let pmc = this.profileHelper.getPmcProfile(sessionId);
        if (pmc != undefined)
        {
            this.itemsNotNeeded = [];
            this.itemsProfile = [];
            this.itemsWishlist = [];
            this.MS2000Markers = Number(0);
            if(pmc?.Inventory) {
                for(let item of pmc.Inventory.items) {
                    var itemProfile = this.itemsProfile[item._tpl]
                    if (itemProfile)
                    {
                        if (item?.upd?.SpawnedInSession) {
                            if (item?.upd?.StackObjectsCount) {
                                itemProfile.stashRaid += item.upd.StackObjectsCount;
                            }
                            else
                            {
                                itemProfile.stashRaid += 1;
                            }
                        }
                        else
                        {
                            if (item?.upd?.StackObjectsCount) {
                                itemProfile.stash += item.upd.StackObjectsCount;
                            }
                            else
                            {
                                itemProfile.stash += 1;
                            }
                        }
                    }
                    else 
                    {
                        if (item?.upd?.SpawnedInSession) {
                            if (item?.upd?.StackObjectsCount) {
                                this.itemsProfile[item._tpl] = {stashRaid: Number(item.upd.StackObjectsCount), stash: Number(0), quest: Number(0)};
                            }
                            else
                            {
                                this.itemsProfile[item._tpl] = {stashRaid: Number(1), stash: Number(0), quest: Number(0)};
                            }
                        }
                        else
                        {
                            if (item?.upd?.StackObjectsCount) {
                                this.itemsProfile[item._tpl] = {stashRaid: Number(0), stash: Number(item.upd.StackObjectsCount), quest: Number(0)};
                            }
                            else
                            {
                                this.itemsProfile[item._tpl] = {stashRaid: Number(0), stash: Number(1), quest: Number(0)};
                            }
                        }
                    }
                }
            }
            if(pmc?.Quests) {
                let quest;
                for (let pmcQuest of pmc.Quests) {
                    if ((pmcQuest.status == 4)) {
                        quest = this.table.templates.quests[pmcQuest.qid];
                        if (quest) {
                            for (let conditionAvailableForFinish of quest.conditions.AvailableForFinish) {
                                if (conditionAvailableForFinish._parent == "HandoverItem" && conditionAvailableForFinish._props.onlyFoundInRaid) {
                                    var itemNotNeeded = this.itemsNotNeeded[conditionAvailableForFinish._props.target];
                                    if (itemNotNeeded) {
                                        itemNotNeeded.quest +=  Number(conditionAvailableForFinish._props.value);
                                    }
                                    else
                                    {
                                        this.itemsNotNeeded[conditionAvailableForFinish._props.target] = {quest: Number(conditionAvailableForFinish._props.value), hideout: Number(0)};
                                    }
                                }
                            }
                        }
                    }
                    if (pmcQuest.status == 2) {
                        quest = this.table.templates.quests[pmcQuest.qid];
                        if (quest) {
                            for (let conditionAvailableForFinish of quest.conditions.AvailableForFinish) {
                                if (conditionAvailableForFinish._parent == "PlaceBeacon") {
                                    this.MS2000Markers +=  Number(1);
                                }
                            }
                        }
                    }
                }
                for (let BackendCounter in pmc.BackendCounters) {
                    quest = this.table.templates.quests[pmc.BackendCounters[BackendCounter].qid]
                    if (quest && pmc.Quests.find(e => e.qid == pmc.BackendCounters[BackendCounter].qid)?.status == 2) {
                        for (let conditionAvailableForFinish of quest.conditions.AvailableForFinish) {
                            if (conditionAvailableForFinish._parent == "HandoverItem" && conditionAvailableForFinish._props.onlyFoundInRaid && conditionAvailableForFinish._props.id == BackendCounter) {
                                var itemProfile = this.itemsProfile[conditionAvailableForFinish._props.target]
                                if (itemProfile) {
                                    itemProfile.stashRaid += Number(pmc.BackendCounters[BackendCounter].value)
                                    //itemNotNeeded.quest +=  Number(pmc.BackendCounters[BackendCounter].value);
                                    //this.logger.success("Quest " + quest.QuestName + " Started " + pmc.BackendCounters[BackendCounter].value + " " + conditionAvailableForFinish._props.target)
                                }
                                else
                                {
                                    this.itemsProfile[conditionAvailableForFinish._props.target] = {stashRaid: Number(pmc.BackendCounters[BackendCounter].value), stash: Number(0), quest: Number(0)};
                                    //this.itemsNotNeeded[conditionAvailableForFinish._props.target] = {quest: Number(pmc.BackendCounters[BackendCounter].value), hideout: Number(0), wishlist: Number(0)};
                                    //this.logger.success("Quest " + quest.QuestName + " Started " + pmc.BackendCounters[BackendCounter].value + " " + conditionAvailableForFinish._props.target)
                                }
                            }
                        }
                    }
                }
            }
            if (pmc?.Hideout?.Areas) {
                for (let area of pmc.Hideout.Areas) {
                    //this.logger.success(area);
                    //this.logger.success(this.table.hideout.areas.find(e => e.type == area.type))
                    for (let stage in this.table.hideout.areas.find(e => e.type == area.type).stages) {
                        if (stage <= area.level) {
                            //this.logger.success(this.table.hideout.areas.find(e => e.type == area.type)._id)
                            //this.logger.success(stage + " " + area.level);
                            for (let requirement of this.table.hideout.areas.find(e => e.type == area.type).stages[stage].requirements) {
                                if (requirement?.type == "Item" && requirement?.templateId && requirement?.count) {
                                    if (this.itemsNotNeeded[requirement.templateId]) {
                                        this.itemsNotNeeded[requirement.templateId].hideout +=  Number(requirement.count);
                                    }
                                    else {
                                        this.itemsNotNeeded[requirement.templateId] = {quest: Number(0), hideout: Number(requirement.count)};
                                    }
                                }
                            }
                        }
                    }
                    //for (let requirement of this.table.hideout.areas.find(e => e.type == area.type).stages[area.level].requirements) {
                    //    if (requirement?.type == "Item" && requirement?.templateId && requirement?.count) {
                    //        if (this.itemsNotNeeded[requirement.templateId]) {
                    //            this.itemsNotNeeded[requirement.templateId].hideout +=  Number(requirement.count);
                    //        }
                    //        else {
                    //            this.itemsNotNeeded[requirement.templateId] = {quest: Number(0), hideout: Number(requirement.count)};
                    //        }
                    //    }
                    //}
                }
            }
            if (pmc?.WishList) {
                for (let itemWislist of pmc.WishList) {
                    this.itemsWishlist[itemWislist] = 1;
                }
            }
        }
    }

    private addRoute() : void
    {
        this.router.registerDynamicRouter(
            "ItemInfodynamicroute",
            [
                {
                    url: "/amanda/iteminfo/",
                    action: (url, info, sessionId, output) =>
                    {
                        return this.onRequestConfig(url, info, sessionId, output)
                    }
                }
            ],
            "ItemInfodynamicroute"
        )
    }
    
    private onRequestConfig(url: string, _info: any, _sessionId: string, _output: string): any
    {
        const splittedUrl = url.split("/");
        const id = splittedUrl[splittedUrl.length - 1].toLowerCase();
        return this.http.noBody(this.itemInfo(id));
    }

    private itemInfo(id: string): any
    {
        let itemInfotrader = 1;
        let itemInfotradertraderMultiplier = {
            traderMultiplier: 1,
            traderName: ""
        };
        let itemInfodurability = 1;
        let parentId = "";
        let itemInfoflea = 1;
        let itemInfocansellonflea = false;
        let itemInfoquest = 0;
        let itemInfostashRaid = 0;
        let itemInfostash = 0;
        let itemInfohideout = 0;
        let itemInfowishlist = 0;
        let itemInfoneeded = this.itemsNeeded[id];
        let itemInfonotNeeded = this.itemsNotNeeded[id];
        let itemInfoProfile = this.itemsProfile[id];
        if (itemInfoneeded) {
            if (typeof(itemInfoneeded.quest) == "number") {
                itemInfoquest = Number(itemInfoneeded.quest);
                //this.logger.success("itemInfoquest number")
            }
            else
            {
                this.logger.error("itemInfoquest number")
            }
            if (typeof(itemInfoneeded.hideout) == "number") {
                itemInfohideout = Number(itemInfoneeded.hideout);
                //this.logger.success("itemInfohideout number")
            }
            else
            {
                this.logger.error("itemInfohideout number")
            }
        }
        if (itemInfonotNeeded) {
            if (typeof(itemInfonotNeeded.quest) == "number") {
                itemInfoquest -= Number(itemInfonotNeeded.quest);
                //this.logger.success("itemInfoquest number")
            }
            else
            {
                this.logger.error("itemInfoquest number")
            }
            if (typeof(itemInfonotNeeded.hideout) == "number") {
                itemInfohideout -= Number(itemInfonotNeeded.hideout);
                //this.logger.success("itemInfohideout number")
            }
            else
            {
                this.logger.error("itemInfohideout number")
            }
        }
        if (itemInfoProfile) {
            if (typeof(itemInfoProfile.stashRaid) == "number") {
                itemInfostashRaid = Number(itemInfoProfile.stashRaid);
                //this.logger.success("itemInfostashRaid number")
            }
            else
            {
                this.logger.error("itemInfostashRaid number")
            }
            if (typeof(itemInfoProfile.stash) == "number") {
                itemInfostash = Number(itemInfoProfile.stash);
                //this.logger.success("itemInfostash number")
            }
            else
            {
                this.logger.error("itemInfostash number")
            }
        }
        if (this.itemsWishlist[id]) {
            itemInfowishlist = Number(1);
        }
        let itemInfofleaTemp = this.livePrice[id];
        if (typeof(itemInfofleaTemp) == "number") {
            itemInfoflea = Number(itemInfofleaTemp);
            //this.logger.success("itemInfoflea number")
        }
        else
        {
            this.logger.error("itemInfoflea number")
        }
        for (const i in this.handbookTable.Items)
        {
            if (this.handbookTable.Items[i].Id === id)
            {
                parentId = this.handbookTable.Items[i].ParentId;
                let itemInfotradertraderMultiplierTemp = this.traderBestMultiplierInfo(parentId);
                let itemInfotraderTemp = this.handbookTable.Items[i].Price;
                let itemInfodurabilityTemp = this.itemDurability(id);
                if (typeof(itemInfotradertraderMultiplierTemp.traderName) == "string" && typeof(itemInfotradertraderMultiplierTemp.traderMultiplier) == "number") {
                    itemInfotradertraderMultiplier = itemInfotradertraderMultiplierTemp;
                    //this.logger.success("traderName string && traderMultiplier number")
                }
                else
                {
                    this.logger.error("traderName string && traderMultiplier number")
                }
                if (typeof(itemInfotraderTemp) == "number") {
                    itemInfotrader = Number(itemInfotraderTemp);
                    //this.logger.success("itemInfotrader number")
                }
                else
                {
                    this.logger.error("itemInfotrader number")
                }
                if (typeof(itemInfodurabilityTemp) == "number") {
                    itemInfodurability = Number(itemInfodurabilityTemp);
                    //this.logger.success("itemInfodurability number")
                }
                else
                {
                    this.logger.error("itemInfodurability number")
                }
            } 
        }
        let itemInfocansellonfleaTemp = this.items[id]?._props?.CanSellOnRagfair;
        if (typeof(itemInfocansellonfleaTemp) == "boolean")
        {
            itemInfocansellonflea = Boolean(itemInfocansellonfleaTemp);
            //this.logger.success("itemInfocansellonflea boolean")
        }
        else
        {
            this.logger.error("itemInfocansellonflea boolean")
        }
        if ((typeof(this.MS2000Markers) == "number") && (id == "5991b51486f77447b112d44f")) {
            itemInfoquest += Number(this.MS2000Markers);
        }
        const result = {
            trader: itemInfotrader,
            multiplier: itemInfotradertraderMultiplier.traderMultiplier,
            name: itemInfotradertraderMultiplier.traderName,
            durability: itemInfodurability,
            flea: itemInfoflea,
            cansellonflea: itemInfocansellonflea,
            quest: itemInfoquest,
            stashRaid: itemInfostashRaid,
            stash: itemInfostash,
            hideout: itemInfohideout,
            wishlist: itemInfowishlist,
        };
        //const result = {
        //    trader: Number(0),
        //    multiplier: Number(0),
        //    name: String(""),
        //    durability: Number(0),
        //    flea: Number(0),
        //    cansellonflea: Boolean(false),
        //    quest: Number(0),
        //    stashRaid: Number(0),
        //    stash: Number(0),
        //    hideout: Number(0),
        //    wishlist: Number(0),
        //};
        //this.logger.success(result)
        return result;
    }

    private traderBestMultiplierInfo(parentId: string): any
    {
        let traderSellCat = "";
        let altTraderSellCat = "";
        let altAltTraderSellCat = "";
        
        for (const i in this.handbookTable.Categories)
        {
            if (this.handbookTable.Categories[i].Id === parentId)
            {
                traderSellCat = this.handbookTable.Categories[i].Id;
                altTraderSellCat = this.handbookTable.Categories[i].ParentId;

                for (const a in this.handbookTable.Categories)
                {
                    if (this.handbookTable.Categories[a].Id === altTraderSellCat)
                    {
                        altAltTraderSellCat = this.handbookTable.Categories[a].ParentId;
                        break;
                    }
                }
                break;
            }
        }

        for (let iter = 0; iter < this.tradersArr.length; iter++)
        {
            if (this.tradersArr[iter].sell_category.includes(traderSellCat))
            {
                return this.traderMultiplierInfo(iter);
            }

            if (this.tradersArr[iter].sell_category.includes(altTraderSellCat))
            {
                return this.traderMultiplierInfo(iter);

            }

            if (this.tradersArr[iter].sell_category.includes(altAltTraderSellCat))
            {
                return this.traderMultiplierInfo(iter);
            }
        }
        const result = {
            traderMultiplier: 1,
            traderName: ""
        }
        return result;
    }

    private traderMultiplierInfo(trader: number): any
    {
        let traderInfoMultiplier = 0.54;
        let traderInfoName = "";
        traderInfoMultiplier = (100 - this.tradersArr[trader].loyaltyLevels[0].buy_price_coef) / 100;
        traderInfoName = this.tradersArr[trader].nickname;
        const result = {
            traderMultiplier: traderInfoMultiplier,
            traderName: traderInfoName
        }
        return result;
    }

    private itemDurability(item: string): number
    {
        if (this.items[item]?._props?.MaxHpResource)
        {
            return this.items[item]._props.MaxHpResource;
        }

        if (this.items[item]?._props?.MaxDurability)
        {
            return this.items[item]._props.MaxDurability;
        }

        if (this.items[item]?._props?.MaxResource)
        {
            return this.items[item]._props.MaxResource;
        }

        if (this.items[item]?._props?.Durability)
        {
            return this.items[item]._props.Durability;
        }

        if (this.items[item]?._props?.MaxRepairResource)
        {
            return this.items[item]._props.MaxRepairResource;
        }
        return 1;
    }
    
}

module.exports = { mod: new ItemInfo() }
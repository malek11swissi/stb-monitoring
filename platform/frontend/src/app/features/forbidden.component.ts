/** Page affichée lorsqu'un utilisateur authentifié ne possède pas le droit demandé. */
import {Component} from '@angular/core';import {RouterLink} from '@angular/router';
@Component({standalone:true,imports:[RouterLink],template:`<section class="page"><div class="card"><h1>Accès refusé</h1><p>Vous ne disposez pas de la permission nécessaire.</p><a routerLink="/dashboard">Retour au tableau de bord</a></div></section>`})export class ForbiddenComponent{}

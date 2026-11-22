import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth.guards';
import { Shell } from './layout/shell/shell';
import { ActivityLog } from './pages/activity-log/activity-log';
import { CandidateDetail } from './pages/candidate-detail/candidate-detail';
import { CandidateSummaryPage } from './pages/candidate-summary/candidate-summary';
import { CandidateForm } from './pages/candidate-form/candidate-form';
import { Candidates } from './pages/candidates/candidates';
import { Dashboard } from './pages/dashboard/dashboard';
import { EvaluationDetail } from './pages/evaluation-detail/evaluation-detail';
import { EvaluationNew } from './pages/evaluation-new/evaluation-new';
import { EvaluationPrint } from './pages/evaluation-print/evaluation-print';
import { Evaluations } from './pages/evaluations/evaluations';
import { JoiningList } from './pages/joining-list/joining-list';
import { Login } from './pages/login/login';
import { MasterData } from './pages/master-data/master-data';
import { Reports } from './pages/reports/reports';
import { ResignedList } from './pages/resigned-list/resigned-list';
import { Settings } from './pages/settings/settings';
import { Unauthorized } from './pages/unauthorized/unauthorized';
import { Users } from './pages/users/users';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', component: Login },
  { path: 'unauthorized', component: Unauthorized },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        component: Dashboard,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.view' }
      },
      {
        path: 'candidates',
        component: Candidates,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.view' }
      },
      {
        path: 'candidates/new',
        component: CandidateForm,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.create' }
      },
      {
        path: 'candidates/:id',
        component: CandidateDetail,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.view' }
      },
      {
        path: 'candidates/:id/summary',
        component: CandidateSummaryPage,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.view' }
      },
      {
        path: 'candidates/:id/edit',
        component: CandidateForm,
        canActivate: [permissionGuard],
        data: { permission: 'candidate.update' }
      },
      {
        path: 'evaluations',
        component: Evaluations,
        canActivate: [permissionGuard],
        data: { permission: 'evaluation.view' }
      },
      {
        path: 'evaluations/new',
        component: EvaluationNew,
        canActivate: [permissionGuard],
        data: { permission: 'evaluation.generate' }
      },
      {
        path: 'evaluations/:id',
        component: EvaluationDetail,
        canActivate: [permissionGuard],
        data: { permission: 'evaluation.view' }
      },
      {
        path: 'evaluations/:id/print',
        component: EvaluationPrint,
        canActivate: [permissionGuard],
        data: { permission: 'evaluation.view' }
      },
      {
        path: 'joining',
        component: JoiningList,
        canActivate: [permissionGuard]
      },
      {
        path: 'resigned',
        component: ResignedList,
        canActivate: [permissionGuard]
      },
      {
        path: 'reports',
        component: Reports,
        canActivate: [permissionGuard],
        data: { permission: 'report.view' }
      },
      {
        path: 'users',
        component: Users,
        canActivate: [permissionGuard],
        data: { permission: 'user.manage' }
      },
      {
        path: 'activity-log',
        component: ActivityLog,
        canActivate: [permissionGuard],
        data: { permission: 'activity.log.view' }
      },
      {
        path: 'master-data',
        component: MasterData,
        canActivate: [permissionGuard],
        data: { permission: 'settings.manage' }
      },
      {
        path: 'settings',
        component: Settings,
        canActivate: [permissionGuard],
        data: { permission: 'settings.manage' }
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];

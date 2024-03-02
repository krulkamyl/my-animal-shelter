<?php

use App\Enums\ShelterUserRole;
use App\Models\Shelter;
use App\Models\User;
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('shelter_user', function (Blueprint $table) {
            $table->id();
            $table->unsignedBigInteger('shelter_id');
            $table->unsignedBigInteger('user_id');

            $table
                ->string('role')
                ->default(ShelterUserRole::VOLUNTEER);

            $table->timestamps();

            $table
                ->foreign('shelter_id')
                ->references('id')
                ->on((new Shelter())->getTable())
                ->onDelete('cascade');

            $table
                ->foreign('user_id')
                ->references('id')
                ->on((new User())->getTable())
                ->onDelete('cascade');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('shelter_user_permissions');
    }
};
